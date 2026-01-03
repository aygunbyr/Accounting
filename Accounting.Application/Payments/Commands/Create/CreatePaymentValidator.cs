using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Utils;
using Accounting.Application.Common.Validation;  // ✅ CommonValidationRules
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Payments.Commands.Create;

public class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    private readonly IAppDbContext _db;

    public CreatePaymentValidator(IAppDbContext db)
    {
        _db = db;

        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.AccountId).GreaterThan(0);
        RuleFor(x => x.Direction).IsInEnum();

        // ✅ CommonValidationRules kullan
        RuleFor(x => x.Currency).MustBeValidCurrency();
        RuleFor(x => x.Amount).MustBeValidMoneyAmount();
        RuleFor(x => x.DateUtc).MustBeValidUtcDateTime();

        // ✅ YENİ: LinkedInvoiceId Validasyonu
        When(x => x.LinkedInvoiceId.HasValue, () =>
        {
            // 1. Invoice exist check
            RuleFor(x => x.LinkedInvoiceId!.Value)
                .MustAsync(async (invoiceId, ct) =>
                {
                    return await _db.Invoices.AnyAsync(i => i.Id == invoiceId && !i.IsDeleted, ct);
                })
                .WithMessage("Linked invoice not found or has been deleted.");

            // 2. Currency match
            RuleFor(x => x)
                .MustAsync(async (cmd, ct) =>
                {
                    if (!cmd.LinkedInvoiceId.HasValue)
                        return true;

                    var invoice = await _db.Invoices
                        .AsNoTracking()
                        .Where(i => i.Id == cmd.LinkedInvoiceId.Value)
                        .Select(i => new { i.Currency })
                        .FirstOrDefaultAsync(ct);

                    if (invoice == null) return true;

                    var paymentCurrency = (cmd.Currency ?? "TRY").ToUpperInvariant();
                    return invoice.Currency.ToUpperInvariant() == paymentCurrency;
                })
                .WithMessage("Payment currency must match invoice currency.");

            // 3. Amount <= Balance
            RuleFor(x => x)
                .MustAsync(async (cmd, ct) =>
                {
                    if (!Money.TryParse2(cmd.Amount, out var amount))
                        return true;

                    if (!cmd.LinkedInvoiceId.HasValue)
                        return true;

                    var invoice = await _db.Invoices
                        .AsNoTracking()
                        .Where(i => i.Id == cmd.LinkedInvoiceId.Value)
                        .Select(i => new { i.Balance })
                        .FirstOrDefaultAsync(ct);

                    if (invoice == null) return true;

                    return amount <= invoice.Balance;
                })
                .WithMessage(cmd =>
                {
                    if (!cmd.LinkedInvoiceId.HasValue)
                        return "Payment amount exceeds invoice balance.";

                    var invoice = _db.Invoices
                        .AsNoTracking()
                        .Where(i => i.Id == cmd.LinkedInvoiceId.Value)
                        .Select(i => new { i.Balance })
                        .FirstOrDefault();

                    if (invoice == null)
                        return "Payment amount exceeds invoice balance.";

                    return $"Payment amount exceeds invoice balance. Remaining balance: {Money.S2(invoice.Balance)} {cmd.Currency ?? "TRY"}";
                });
        });
    }
}