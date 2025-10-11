using Accounting.Application.Common.Utils;
using FluentValidation;

namespace Accounting.Application.Items.Commands.Create;

public class CreateItemValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(16);
        RuleFor(x => x.VatRate).InclusiveBetween(0, 100);
        // DefaultUnitPrice => optional ama varsa parse edilmeli
        RuleFor(x => x.DefaultUnitPrice)
            .Must(p => p is null || Money.TryParse2(p, out _))
            .WithMessage("DefaultUnitPrice formatı geçersiz.");
    }
}
