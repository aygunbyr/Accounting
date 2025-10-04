using FluentValidation;

public class UpdatePaymentValidator : AbstractValidator<UpdatePaymentCommand>
{
    public UpdatePaymentValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.AccountId).GreaterThan(0);
        RuleFor(x => x.Direction).IsInEnum();
        RuleFor(x => x.Amount).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.DateUtc).NotEmpty();

        When(x => x.ContactId.HasValue, () =>
        {
            RuleFor(x => x.ContactId!.Value).GreaterThan(0);
        });
        When(x => x.LinkedInvoiceId.HasValue, () =>
        {
            RuleFor(x => x.LinkedInvoiceId!.Value).GreaterThan(0);
        });
    }
}
