using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Cheques.Commands.Create;

public class CreateChequeHandler(IAppDbContext db) : IRequestHandler<CreateChequeCommand, int>
{
    public async Task<int> Handle(CreateChequeCommand request, CancellationToken ct)
    {
        // Duplicate check: Aynı şubede aynı çek numarası olamaz
        var exists = await db.Cheques.AnyAsync(c =>
            c.BranchId == request.BranchId &&
            c.ChequeNumber == request.ChequeNumber &&
            !c.IsDeleted, ct);

        if (exists)
            throw new BusinessRuleException($"Bu çek numarası ({request.ChequeNumber}) zaten kayıtlı.");

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
