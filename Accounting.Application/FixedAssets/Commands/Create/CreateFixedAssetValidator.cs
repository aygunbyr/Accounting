using FluentValidation;

namespace Accounting.Application.FixedAssets.Commands.Create;

public sealed class CreateFixedAssetValidator
    : AbstractValidator<CreateFixedAssetCommand>
{
    public CreateFixedAssetValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.PurchasePrice).GreaterThan(0m);
        RuleFor(x => x.UsefulLifeYears).InclusiveBetween(1, 100);

        RuleFor(x => x.PurchaseDateUtc)
            .Must(d => d.Kind == DateTimeKind.Utc)
            .WithMessage("PurchaseDateUtc must be in UTC.");
    }
}
