using FluentValidation;

namespace Accounting.Application.Invoices.Commands.Create;

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.ContactId).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.DateUtc).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).GreaterThan(0);

            line.RuleFor(l => l.Qty)
                .NotEmpty()
                .Matches(@"^\d+(\.\d{1,3})?$")
                .WithMessage("Qty must be a decimal with up to 3 fraction digits.");

            line.RuleFor(l => l.UnitPrice)
                .NotEmpty()
                .Matches(@"^\d+(\.\d{1,4})?$")
                .WithMessage("UnitPrice must be a decimal with up to 4 fraction digits.");

            line.RuleFor(l => l.VatRate)
                .InclusiveBetween(0, 100);
        });

    }
}
