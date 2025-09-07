using FluentValidation;

namespace Accounting.Application.Expenses.Commands.PostToBill;

public class PostExpenseListToBillValidator : AbstractValidator<PostExpenseListToBillCommand>
{
    public PostExpenseListToBillValidator()
    {
        RuleFor(x => x.ExpenseListId).GreaterThan(0);
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.ItemId).GreaterThan(0);
        // DateUtc opsiyonel; sağlanırsa ISO-8601 olmalı
    }
}
