using System.Globalization;
using FluentValidation;

namespace Accounting.Application.Invoices.Commands.Create;

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        // Temel alanlar
        RuleFor(x => x.ContactId)
            .GreaterThan(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("Currency 3 karakter olmalı (örn: TRY).");

        RuleFor(x => x.DateUtc)
            .NotEmpty()
            .Must(BeIso8601Utc)
            .WithMessage("DateUtc ISO-8601 UTC olmalı (örn: 2025-09-07T10:00:00Z).");

        RuleFor(x => x.Type)
            .IsInEnum();

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
