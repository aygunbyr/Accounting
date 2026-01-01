using Accounting.Application.Common.Validation;
using FluentValidation;

public class SoftDeletePaymentValidator : AbstractValidator<SoftDeletePaymentCommand>
{
    public SoftDeletePaymentValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.RowVersion).MustBeValidRowVersion();
    }
}
