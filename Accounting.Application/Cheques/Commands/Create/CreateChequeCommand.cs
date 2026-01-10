using Accounting.Domain.Enums;
using MediatR;

namespace Accounting.Application.Cheques.Commands.Create;

public record CreateChequeCommand(
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
