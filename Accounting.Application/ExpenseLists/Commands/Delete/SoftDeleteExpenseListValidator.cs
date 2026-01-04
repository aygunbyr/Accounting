using FluentValidation;

namespace Accounting.Application.ExpenseLists.Commands.Delete;

public class SoftDeleteExpenseListValidator : AbstractValidator<SoftDeleteExpenseListCommand>
{
    public SoftDeleteExpenseListValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("RowVersion is required for concurrency control.");
    }
}