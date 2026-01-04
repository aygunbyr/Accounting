using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Application.Common.Utils;
using Accounting.Application.Expenses.Queries.Dto;
// CreateInvoiceCommand/Dto referansları:
using Accounting.Application.Invoices.Commands.Create;
using Accounting.Application.Payments.Commands.Create;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;

namespace Accounting.Application.Expenses.Commands.PostToBill;

public class PostExpenseListToBillHandler
    : IRequestHandler<PostExpenseListToBillCommand, PostExpenseListToBillResult>
{
    private readonly IAppDbContext _db;
    private readonly IMediator _mediator;

    public PostExpenseListToBillHandler(IAppDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<PostExpenseListToBillResult> Handle(PostExpenseListToBillCommand req, CancellationToken ct)
    {
        // Liste + satırlar
        var list = await _db.ExpenseLists
            .Include(x => x.Branch)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == req.ExpenseListId, ct);

        if (list is null)
            throw new KeyNotFoundException($"ExpenseList {req.ExpenseListId} not found.");

        if (list.Status != ExpenseListStatus.Reviewed)
            throw new BusinessRuleException("Only Reviewed lists can be posted to bill.");

        // ✅ YENİ: Duplikasyon kontrolü
        if (list.PostedInvoiceId.HasValue)
            throw new BusinessRuleException(
                $"Expense list already posted to invoice #{list.PostedInvoiceId.Value}. Cannot post again.");

        if (!list.Lines.Any())
            throw new InvalidOperationException("Expense list has no lines.");

        // Para birimi bütünlüğü
        var distinctCurrencies = list.Lines.Select(l => l.Currency).Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
        if (distinctCurrencies.Count != 1 || !string.Equals(distinctCurrencies[0], req.Currency, StringComparison.InvariantCultureIgnoreCase))
            throw new InvalidOperationException("All expense lines must share the same currency and match the requested currency.");

        // Tedarikçi bütünlüğü
        var nonNullSuppliers = list.Lines.Where(l => l.SupplierId.HasValue).Select(l => l.SupplierId!.Value).Distinct().ToList();
        if (nonNullSuppliers.Count > 1 && nonNullSuppliers.Any(s => s != req.SupplierId))
            throw new InvalidOperationException("Expense lines have multiple suppliers; please normalize before posting.");

        // Fatura tarihi (UTC)
        DateTime dateUtc;
        if (!string.IsNullOrWhiteSpace(req.DateUtc))
        {
            if (!DateTime.TryParse(req.DateUtc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dateUtc))
                throw new ArgumentException("DateUtc is invalid.");
        }
        else
        {
            dateUtc = DateTime.UtcNow;
        }

        // CreateInvoiceCommand (yeniden kullanım)
        var lines = list.Lines.Select(l => new CreateInvoiceLineDto(
            ItemId: req.ItemId,
            Qty: "1.000",
            UnitPrice: Money.S2(l.Amount),  // ✅ F2 kullan (Amount decimal(18,2))
            VatRate: l.VatRate
        )).ToList();

        var createCmd = new CreateInvoiceCommand(
            BranchId: list.BranchId,
            ContactId: req.SupplierId,
            DateUtc: dateUtc.ToString("o", CultureInfo.InvariantCulture),
            Currency: req.Currency.ToUpperInvariant(),
            Lines: lines,
            Type: InvoiceType.Purchase.ToString()
        );

        var created = await _mediator.Send(createCmd, ct);

        // ✅ YENİ: CreatePayment ise otomatik ödeme oluştur
        if (req.CreatePayment)
        {
            if (!req.PaymentAccountId.HasValue)
                throw new BusinessRuleException("PaymentAccountId is required when CreatePayment is true.");

            var paymentCmd = new CreatePaymentCommand(
                BranchId: list.BranchId,
                AccountId: req.PaymentAccountId.Value,
                ContactId: req.SupplierId,
                LinkedInvoiceId: created.Id,
                Direction: PaymentDirection.Out,  // Purchase → Out (ödeme yapıyoruz)
                Amount: created.TotalGross,  // Invoice total
                Currency: req.Currency.ToUpperInvariant(),
                DateUtc: req.PaymentDateUtc ?? dateUtc.ToString("o", CultureInfo.InvariantCulture)
            );

            await _mediator.Send(paymentCmd, ct);
        }

        // Listeyi "Posted" işaretle ve satırlara InvoiceId yaz
        list.Status = ExpenseListStatus.Posted;
        list.PostedInvoiceId = created.Id;

        foreach (var l in list.Lines)
            l.PostedInvoiceId = created.Id;

        await _db.SaveChangesAsync(ct);

        return new PostExpenseListToBillResult(
            CreatedInvoiceId: created.Id,
            PostedExpenseCount: list.Lines.Count
        );
    }
}