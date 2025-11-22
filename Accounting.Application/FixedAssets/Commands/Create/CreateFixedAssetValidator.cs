using FluentValidation;

namespace Accounting.Application.FixedAssets.Commands.Create;

public sealed class CreateFixedAssetValidator
    : AbstractValidator<CreateFixedAssetCommand>
{
    public CreateFixedAssetValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.PurchasePrice)
            .GreaterThan(0m);

        RuleFor(x => x.UsefulLifeYears)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.PurchaseDateUtc)
            .NotEmpty();
    }
}
