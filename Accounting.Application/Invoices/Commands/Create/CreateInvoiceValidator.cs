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

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).GreaterThan(0);
            line.RuleFor(l => l.Qty).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.VatRate).InclusiveBetween(0, 100);
        });
    }
}
