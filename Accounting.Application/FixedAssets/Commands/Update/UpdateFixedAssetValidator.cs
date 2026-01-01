using Accounting.Application.Common.Validation;
using FluentValidation;

namespace Accounting.Application.FixedAssets.Commands.Update;

public sealed class UpdateFixedAssetValidator
    : AbstractValidator<UpdateFixedAssetCommand>
{
    public UpdateFixedAssetValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.RowVersionBase64).MustBeValidRowVersion();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.PurchasePrice).GreaterThan(0m);
        RuleFor(x => x.UsefulLifeYears).InclusiveBetween(1, 100);
        RuleFor(x => x.PurchaseDateUtc).NotEmpty();
    }
}
