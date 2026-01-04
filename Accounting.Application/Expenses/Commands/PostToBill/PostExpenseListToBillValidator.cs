using Accounting.Application.Common.Validation;
using FluentValidation;

namespace Accounting.Application.Expenses.Commands.PostToBill;

public class PostExpenseListToBillValidator : AbstractValidator<PostExpenseListToBillCommand>
{
    public PostExpenseListToBillValidator()
    {
        RuleFor(x => x.ExpenseListId).GreaterThan(0);
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.ItemId).GreaterThan(0);

        RuleFor(x => x.Currency).MustBeValidCurrency();

        // CreatePayment=true ise PaymentAccountId zorunlu
        When(x => x.CreatePayment, () =>
        {
            RuleFor(x => x.PaymentAccountId)
                .NotNull()
                .WithMessage("PaymentAccountId is required when CreatePayment is true.");

            RuleFor(x => x.PaymentAccountId!.Value)
                .GreaterThan(0)
                .WithMessage("PaymentAccountId must be a valid account ID.");
        });


        // DateUtc (optional ama dolu ise geçerli olmalı)
        When(x => !string.IsNullOrWhiteSpace(x.DateUtc), () =>
        {
            RuleFor(x => x.DateUtc).MustBeValidUtcDateTime();
        });

        // PaymentDateUtc (optional ama dolu ise geçerli olmalı)
        When(x => !string.IsNullOrWhiteSpace(x.PaymentDateUtc), () =>
        {
            RuleFor(x => x.PaymentDateUtc).MustBeValidUtcDateTime();
        });
    }
}