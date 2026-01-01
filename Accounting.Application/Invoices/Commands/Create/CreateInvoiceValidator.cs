using Accounting.Application.Common.Validation;
using Accounting.Domain.Entities;
using FluentValidation;
using System.Globalization;

namespace Accounting.Application.Invoices.Commands.Create;

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        // Temel alanlar
        RuleFor(x => x.ContactId)
            .GreaterThan(0);

        RuleFor(x => x.DateUtc).MustBeValidUtcDateTime();           // Extension
        RuleFor(x => x.Currency).MustBeValidCurrency();             // Extension

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(v => int.TryParse(v, out var n)
                ? Enum.IsDefined(typeof(InvoiceType), n)
                : new[] { "Sales", "Purchase", "SalesReturn", "PurchaseReturn" }
                    .Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Geçersiz fatura türü.");

        // Satırlar
        RuleFor(x => x.Lines)
            .NotNull()
            .Must(l => l.Count > 0)
            .WithMessage("En az bir satır girmelisiniz.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId)
                .GreaterThan(0);

            // string (max 3 ondalık) — örn: "1", "1.2", "1.234"
            line.RuleFor(l => l.Qty)
                .NotEmpty()
                .Matches(@"^\d+(\.\d{1,3})?$")
                .WithMessage("Qty en fazla 3 ondalık haneye sahip bir sayı olmalı (örn: 1.000).");

            // string (max 4 ondalık) — örn: "10", "10.5", "10.1234"
            line.RuleFor(l => l.UnitPrice)
                .NotEmpty()
                .Matches(@"^\d+(\.\d{1,4})?$")
                .WithMessage("UnitPrice en fazla 4 ondalık haneye sahip bir sayı olmalı (örn: 10.0000).");

            line.RuleFor(l => l.VatRate)
                .InclusiveBetween(0, 100);
        });
    }

    private static bool BeIso8601Utc(string iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return false;
        return DateTime.TryParse(
            iso,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out _);
    }
}
