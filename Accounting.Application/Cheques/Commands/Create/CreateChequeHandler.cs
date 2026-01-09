using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using FluentValidation;

namespace Accounting.Application.Cheques.Commands.Create;

public record CreateChequeCommand(
    int BranchId,
    int? ContactId,
    ChequeType Type,
    ChequeDirection Direction,
    string ChequeNumber,
    DateTime IssueDate,
    DateTime DueDate,
    decimal Amount,
    string Currency,
    string? BankName,
    string? BankBranch,
    string? AccountNumber,
    string? DrawerName,
    string? Description
) : IRequest<int>;

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

public class CreateChequeHandler(IAppDbContext db) : IRequestHandler<CreateChequeCommand, int>
{
    public async Task<int> Handle(CreateChequeCommand request, CancellationToken ct)
    {
        var cheque = new Cheque
        {
            BranchId = request.BranchId,
            ContactId = request.ContactId,
            Type = request.Type,
            Direction = request.Direction,
            Status = ChequeStatus.Pending, // Yeni evrak 'Portföyde' başlar
            ChequeNumber = request.ChequeNumber,
            IssueDate = request.IssueDate,
            DueDate = request.DueDate,
            Amount = request.Amount,
            Currency = request.Currency ?? "TRY",
            BankName = request.BankName,
            BankBranch = request.BankBranch,
            AccountNumber = request.AccountNumber,
            DrawerName = request.DrawerName,
            Description = request.Description
        };

        db.Cheques.Add(cheque);
        await db.SaveChangesAsync(ct);

        return cheque.Id;
    }
}
