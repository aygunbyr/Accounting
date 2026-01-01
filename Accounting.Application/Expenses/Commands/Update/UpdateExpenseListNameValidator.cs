using Accounting.Application.Common.Validation;
using FluentValidation;

namespace Accounting.Application.Expenses.Commands.Update;

public class UpdateExpenseListNameValidator : AbstractValidator<UpdateExpenseListNameCommand>
{
    public UpdateExpenseListNameValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RowVersion).MustBeValidRowVersion();
    }
}
