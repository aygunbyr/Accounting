using FluentValidation;

namespace Accounting.Application.Expenses.Commands.AddLine;

public class AddExpenseLineValidator : AbstractValidator<AddExpenseLineCommand>
{
    public AddExpenseLineValidator()
    {
        RuleFor(x => x.ExpenseListId).GreaterThan(0);
        RuleFor(x => x.DateUtc).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Amount).NotEmpty().Matches(@"^\d+(\.\d{1,2})?$")
            .WithMessage("Amount must be a decimal with up to 2 fraction digits.");
        RuleFor(x => x.VatRate).InclusiveBetween(0, 100);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
