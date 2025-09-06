using FluentValidation;

namespace Accounting.Application.Expenses.Commands.Review;

public class ReviewExpenseValidator : AbstractValidator<ReviewExpenseListCommand>
{
    public ReviewExpenseValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
