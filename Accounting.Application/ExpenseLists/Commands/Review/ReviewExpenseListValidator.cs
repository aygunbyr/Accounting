using FluentValidation;

namespace Accounting.Application.ExpenseLists.Commands.Review;

public class ReviewExpenseListValidator : AbstractValidator<ReviewExpenseListCommand>
{
    public ReviewExpenseListValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}