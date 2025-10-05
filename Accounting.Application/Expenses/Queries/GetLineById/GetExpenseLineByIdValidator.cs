using FluentValidation;

namespace Accounting.Application.Expenses.Queries.GetLineById;

public class GetExpenseLineByIdValidator : AbstractValidator<GetExpenseLineByIdQuery>
{
    public GetExpenseLineByIdValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
