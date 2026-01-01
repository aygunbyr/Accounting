using Accounting.Application.Common.Validation;
using FluentValidation;

namespace Accounting.Application.Expenses.Commands.Delete;

public class SoftDeleteExpenseListValidator : AbstractValidator<SoftDeleteExpenseListCommand>
{
    public SoftDeleteExpenseListValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.RowVersion).MustBeValidRowVersion();
    }
}
