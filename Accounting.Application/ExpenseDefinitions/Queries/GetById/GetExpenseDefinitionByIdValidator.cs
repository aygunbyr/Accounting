using FluentValidation;

namespace Accounting.Application.ExpenseDefinitions.Queries.GetById;

public sealed class GetExpenseDefinitionByIdValidator
    : AbstractValidator<GetExpenseDefinitionByIdQuery>
{
    public GetExpenseDefinitionByIdValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}