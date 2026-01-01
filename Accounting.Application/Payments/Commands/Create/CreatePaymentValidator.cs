using Accounting.Application.Common.Utils;
using Accounting.Application.Common.Validation;
using FluentValidation;

namespace Accounting.Application.Payments.Commands.Create;

public class CreatePaymentValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.AccountId).GreaterThan(0);
        RuleFor(x => x.DateUtc).MustBeValidUtcDateTime();           // Extension
        RuleFor(x => x.Direction).IsInEnum();
        RuleFor(x => x.Amount).MustBeValidMoneyAmount();            // Extension
        RuleFor(x => x.Currency).MustBeValidCurrency();             // Extension
    }
}
