using Accounting.Application.Common.Utils;
using FluentValidation;

namespace Accounting.Application.Items.Commands.Create;

public class CreateItemValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(16);
        RuleFor(x => x.VatRate).InclusiveBetween(0, 100);
        // Purchase & Sales Prices optional but must be valid money format
        RuleFor(x => x.PurchasePrice)
            .Must(p => p is null || Money.TryParse2(p, out _))
            .WithMessage("PurchasePrice formatı geçersiz (örn: 100,50)");

        RuleFor(x => x.SalesPrice)
            .Must(p => p is null || Money.TryParse2(p, out _))
            .WithMessage("SalesPrice formatı geçersiz (örn: 120,00)");
    }
}
