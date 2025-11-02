using Accounting.Domain.Entities;
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

            RuleFor(x => x.RowVersionBase64)
                .NotEmpty()
                .Must(BeValidBase64).WithMessage("RowVersionBase64 geçersiz (base64).");

            RuleFor(x => x.DateUtc)
                .Must(d => d.Kind == DateTimeKind.Utc)
                .WithMessage("DateUtc UTC olmalıdır.");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .Must(IsIso4217).WithMessage("Currency 3 harf olmalıdır (ISO 4217, A-Z).");

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

        private static bool BeValidBase64(string base64)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(base64)) return false;
                Convert.FromBase64String(base64);
                return true;
            }
            catch { return false; }
        }

        private static bool IsIso4217(string? currency)
        {
            if (string.IsNullOrWhiteSpace(currency)) return false;
            var c = currency.Trim().ToUpperInvariant();
            if (c.Length != 3) return false;
            return c.All(ch => ch >= 'A' && ch <= 'Z');
        }
    }

    internal sealed class UpdateInvoiceLineValidator : AbstractValidator<UpdateInvoiceLineDto>
    {
        public UpdateInvoiceLineValidator()
        {
            RuleFor(l => l.Id).GreaterThanOrEqualTo(0);
            RuleFor(l => l.ItemId).GreaterThan(0);
            RuleFor(l => l.Qty).GreaterThan(0m).WithMessage("Qty 0'dan büyük olmalıdır.");
            RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0m);
            RuleFor(l => l.VatRate).InclusiveBetween(0, 100);
        }
    }
}
