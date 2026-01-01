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
            line.RuleFor(l => l.Qty).MustBeValidQuantity();        // Extension
            line.RuleFor(l => l.UnitPrice).MustBeValidUnitPrice(); // Extension

            line.RuleFor(l => l.VatRate)
                .InclusiveBetween(0, 100);
        });
    }
}
