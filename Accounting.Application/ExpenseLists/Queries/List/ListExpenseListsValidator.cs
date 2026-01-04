using FluentValidation;

namespace Accounting.Application.ExpenseLists.Queries.List;

public class ListExpenseListsValidator : AbstractValidator<ListExpenseListsQuery>
{
    public ListExpenseListsValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}