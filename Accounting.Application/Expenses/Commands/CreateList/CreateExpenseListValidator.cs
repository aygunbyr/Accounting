using FluentValidation;

namespace Accounting.Application.Expenses.Commands.CreateList;

public class CreateExpenseListValidator : AbstractValidator<CreateExpenseListCommand>
{
    public CreateExpenseListValidator()
    {
        RuleFor(x => x.Name).MaximumLength(200);
    }
}
