using Accounting.Application.Common.Abstractions;
using Accounting.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Orders.Commands.Create;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    private readonly IAppDbContext _db;

    public CreateOrderValidator(IAppDbContext db)
    {
        _db = db;

        RuleFor(x => x.BranchId)
            .GreaterThan(0)
            .MustAsync(BranchExistsAsync).WithMessage("Şube bulunamadı.");

        RuleFor(x => x.ContactId)
            .GreaterThan(0)
            .MustAsync(ContactExistsAsync).WithMessage("Cari bulunamadı.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .Must(t => t != InvoiceType.Expense)
            .WithMessage("Masraf faturası için sipariş oluşturulamaz.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .MaximumLength(3);

        RuleFor(x => x.DateUtc)
            .NotEmpty();

        RuleFor(x => x.Description)
            .MaximumLength(200);

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("En az bir sipariş kalemi gereklidir.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description)
                .NotEmpty()
                .MaximumLength(200);

            line.RuleFor(l => l.Quantity)
                .NotEmpty()
                .Must(q => decimal.TryParse(q.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var val) && val > 0)
                .WithMessage("Miktar sıfırdan büyük olmalıdır.");

            line.RuleFor(l => l.UnitPrice)
                .NotEmpty()
                .Must(p => decimal.TryParse(p.Replace(",", "."),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var val) && val >= 0)
                .WithMessage("Birim fiyat negatif olamaz.");

            line.RuleFor(l => l.VatRate)
                .Must(v => v == 0 || v == 1 || v == 10 || v == 20)
                .WithMessage("KDV oranı 0, 1, 10 veya 20 olmalıdır.");
        });
    }

    private async Task<bool> BranchExistsAsync(int branchId, CancellationToken ct)
    {
        return await _db.Branches.AnyAsync(b => b.Id == branchId && !b.IsDeleted, ct);
    }

    private async Task<bool> ContactExistsAsync(int contactId, CancellationToken ct)
    {
        return await _db.Contacts.AnyAsync(c => c.Id == contactId && !c.IsDeleted, ct);
    }
}
