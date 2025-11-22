using FluentValidation;

namespace Accounting.Application.FixedAssets.Queries.GetById;

public sealed class GetFixedAssetByIdValidator
    : AbstractValidator<GetFixedAssetByIdQuery>
{
    public GetFixedAssetByIdValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
