using Accounting.Application.Common.Validation;
using FluentValidation;

namespace Accounting.Application.FixedAssets.Commands.Delete;

public sealed class DeleteFixedAssetValidator
    : AbstractValidator<DeleteFixedAssetCommand>
{
    public DeleteFixedAssetValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.RowVersionBase64).MustBeValidRowVersion();
    }
}
