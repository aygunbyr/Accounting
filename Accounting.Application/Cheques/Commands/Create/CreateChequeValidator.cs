using Accounting.Domain.Enums;
using FluentValidation;

namespace Accounting.Application.Cheques.Commands.Create;

public class CreateChequeValidator : AbstractValidator<CreateChequeCommand>
{
    public CreateChequeValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0);
        RuleFor(x => x.ChequeNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.IssueDate).WithMessage("Vade tarihi düzenleme tarihinden önce olamaz.");

        // Müşteri çeki ise Contact zorunlu
        RuleFor(x => x.ContactId)
            .NotEmpty()
            .When(x => x.Direction == ChequeDirection.Inbound)
            .WithMessage("Müşteri evrağı girişinde cari seçimi zorunludur.");
    }
}
