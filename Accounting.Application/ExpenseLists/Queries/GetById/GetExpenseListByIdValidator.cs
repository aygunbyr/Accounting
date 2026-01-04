using FluentValidation;

namespace Accounting.Application.ExpenseLists.Queries.GetById;

public class GetExpenseListByIdValidator : AbstractValidator<GetExpenseListByIdQuery>
{
    public GetExpenseListByIdValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}