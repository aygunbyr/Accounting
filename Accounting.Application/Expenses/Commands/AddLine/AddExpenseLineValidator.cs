using Accounting.Application.Common.Validation;
using FluentValidation;

namespace Accounting.Application.Expenses.Commands.AddLine;

public class AddExpenseLineValidator : AbstractValidator<AddExpenseLineCommand>
{
    public AddExpenseLineValidator()
    {
        RuleFor(x => x.ExpenseListId).GreaterThan(0);
        RuleFor(x => x.DateUtc).MustBeValidUtcDateTime();  // Extension
        RuleFor(x => x.Currency).MustBeValidCurrency();    // Extension
        RuleFor(x => x.Amount).MustBeValidMoneyAmount();   // Extension
        RuleFor(x => x.VatRate).InclusiveBetween(0, 100);
        RuleFor(x => x.Category).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
