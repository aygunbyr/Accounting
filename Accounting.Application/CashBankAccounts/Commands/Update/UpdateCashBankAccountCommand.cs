using Accounting.Application.CashBankAccounts.Queries.Dto;
using Accounting.Application.Common.Abstractions;
using Accounting.Domain.Enums;
using MediatR;

namespace Accounting.Application.CashBankAccounts.Commands.Update;

public record UpdateCashBankAccountCommand(
    int Id,
    CashBankAccountType Type,   // <-- enum (Cash|Bank)
    string Name,
    string? Iban,
    string RowVersion
) : IRequest<CashBankAccountDetailDto>;
