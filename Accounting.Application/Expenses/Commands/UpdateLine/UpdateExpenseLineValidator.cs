using FluentValidation;

namespace Accounting.Application.Expenses.Commands.UpdateLine;

public class UpdateExpenseLineValidator : AbstractValidator<UpdateExpenseLineCommand>
{
    public UpdateExpenseLineValidator()
    {
        RuleFor(x => x.LineId).GreaterThan(0);
        RuleFor(x => x.ExpenseListId).GreaterThan(0);
        RuleFor(x => x.RowVersion).NotEmpty();

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);

        RuleFor(x => x.Amount)
            .NotEmpty()
            .Matches(@"^-?\d+(\.\d{1,})?$");

        RuleFor(x => x.VatRate)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.Category).MaximumLength(100)
            .When(x => x.Category != null);

        RuleFor(x => x.Notes).MaximumLength(500)
            .When(x => x.Notes != null);
    }
}
