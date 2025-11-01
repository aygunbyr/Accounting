using FluentValidation;
using System.Globalization;

namespace Accounting.Application.Invoices.Commands.UpdateHeader;

public class UpdateInvoiceHeaderValidator : AbstractValidator<UpdateInvoiceHeaderCommand>
{
    public UpdateInvoiceHeaderValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ContactId).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.RowVersion).NotEmpty();

        RuleFor(x => x.DateUtc)
            .NotEmpty()
            .Must(s => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out _))
            .WithMessage("DateUtc ISO-8601 (UTC) olmalı.");
    }
}
