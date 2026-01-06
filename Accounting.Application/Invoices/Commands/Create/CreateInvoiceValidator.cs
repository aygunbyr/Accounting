using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Validation;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Accounting.Application.Invoices.Commands.Create;

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    private readonly IAppDbContext _db;

    public CreateInvoiceValidator(IAppDbContext db)
    {
        _db = db;

        // Temel alanlar
        RuleFor(x => x.BranchId)
            .GreaterThan(0)
            .MustAsync(BranchExistsAsync)
            .WithMessage("Şube bulunamadı.");

        RuleFor(x => x.ContactId)
            .GreaterThan(0);

        RuleFor(x => x.DateUtc).MustBeValidUtcDateTime();           // Extension
        RuleFor(x => x.Currency).MustBeValidCurrency();             // Extension

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(v => int.TryParse(v, out var n)
                ? Enum.IsDefined(typeof(InvoiceType), n)
                : new[] { "Sales", "Purchase", "SalesReturn", "PurchaseReturn", "Expense" }
                    .Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Geçersiz fatura türü.");

        // ✅ Branch Tutarlılık Kontrolü: Contact aynı şubeye ait olmalı
        RuleFor(x => x)
            .MustAsync(ContactBelongsToSameBranchAsync)
            .WithMessage("Cari (Contact) fatura ile aynı şubeye ait olmalıdır.")
            .When(x => x.ContactId > 0 && x.BranchId > 0);

        // Satırlar
        RuleFor(x => x.Lines)
            .NotNull()
            .Must(l => l.Count > 0)
            .WithMessage("En az bir satır girmelisiniz.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId)
                .GreaterThan(0)
                .When(l => l.ItemId.HasValue);
            line.RuleFor(l => l.Qty).MustBeValidQuantity();        // Extension
            line.RuleFor(l => l.UnitPrice).MustBeValidUnitPrice(); // Extension

            line.RuleFor(l => l.VatRate)
                .InclusiveBetween(0, 100);
        });

        // ✅ Branch Tutarlılık Kontrolü: Satırlardaki Item'lar aynı şubeye ait olmalı
        RuleFor(x => x)
            .MustAsync(AllItemsBelongToSameBranchAsync)
            .WithMessage("Fatura satırlarındaki ürünler (Item) fatura ile aynı şubeye ait olmalıdır.")
            .When(x => x.BranchId > 0 && x.Lines != null && x.Lines.Any(l => l.ItemId.HasValue));
    }

    private async Task<bool> BranchExistsAsync(int branchId, CancellationToken ct)
    {
        return await _db.Branches.AnyAsync(b => b.Id == branchId && !b.IsDeleted, ct);
    }

    private async Task<bool> ContactBelongsToSameBranchAsync(CreateInvoiceCommand cmd, CancellationToken ct)
    {
        var contact = await _db.Contacts
            .AsNoTracking()
            .Where(c => c.Id == cmd.ContactId && !c.IsDeleted)
            .Select(c => new { c.BranchId })
            .FirstOrDefaultAsync(ct);

        if (contact == null)
            return false; // Contact bulunamadı - ayrı validasyonda yakalanabilir

        return contact.BranchId == cmd.BranchId;
    }

    private async Task<bool> AllItemsBelongToSameBranchAsync(CreateInvoiceCommand cmd, CancellationToken ct)
    {
        var itemIds = cmd.Lines
            .Where(l => l.ItemId.HasValue)
            .Select(l => l.ItemId!.Value)
            .Distinct()
            .ToList();

        if (!itemIds.Any())
            return true; // Item yoksa (ExpenseDefinition kullanılıyor olabilir)

        var mismatchedItems = await _db.Items
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id) && !i.IsDeleted && i.BranchId != cmd.BranchId)
            .AnyAsync(ct);

        return !mismatchedItems;
    }
}
