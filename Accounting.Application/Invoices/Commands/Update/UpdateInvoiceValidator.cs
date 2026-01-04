using Accounting.Application.Common.Validation;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using FluentValidation;
using System;
using System.Linq;

namespace Accounting.Application.Invoices.Commands.Update
{
    public sealed class UpdateInvoiceValidator : AbstractValidator<UpdateInvoiceCommand>
    {
        public UpdateInvoiceValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);

            RuleFor(x => x.RowVersionBase64).MustBeValidRowVersion();  // Extension

            RuleFor(x => x.DateUtc)
                .Must(d => d.Kind == DateTimeKind.Utc)
                .WithMessage("DateUtc UTC olmalıdır.");

            RuleFor(x => x.Currency).MustBeValidCurrency();            // Extension

            RuleFor(x => x.ContactId).GreaterThan(0);

            RuleFor(x => x.Lines)
                .NotNull().WithMessage("Lines null olamaz.")
                .Must(l => l != null && l.Count > 0).WithMessage("En az bir satır olmalıdır.");

            RuleFor(x => x.Type)
                .NotEmpty()
                .Must(v => int.TryParse(v, out var n) ? Enum.IsDefined(typeof(InvoiceType), n)
                                        : new[] { "Sales", "Purchase", "SalesReturn", "PurchaseReturn" }
                                          .Contains(v, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Geçersiz fatura türü.");

            // Id>0 olan satırlarda tekrar kontrolü
            RuleFor(x => x.Lines)
                .Must(lines =>
                {
                    var ids = lines.Where(l => l.Id > 0).Select(l => l.Id);
                    return ids.Distinct().Count() == ids.Count();
                })
                .WithMessage("Lines içinde tekrar eden satır Id değerleri var.");

            RuleForEach(x => x.Lines).SetValidator(new UpdateInvoiceLineValidator());
        }
    }

    internal sealed class UpdateInvoiceLineValidator : AbstractValidator<UpdateInvoiceLineDto>
    {
        public UpdateInvoiceLineValidator()
        {
            RuleFor(l => l.Id).GreaterThanOrEqualTo(0);
            RuleFor(l => l.ItemId).GreaterThan(0);
            RuleFor(l => l.Qty).MustBeValidQuantity();        // Extension
            RuleFor(l => l.UnitPrice).MustBeValidUnitPrice(); // Extension
            RuleFor(l => l.VatRate).InclusiveBetween(0, 100);
        }
    }
}
