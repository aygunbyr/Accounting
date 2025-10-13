using Accounting.Application.CashBankAccounts.Queries.Dto;
using Accounting.Application.Common.Abstractions;
using Accounting.Domain.Entities;
using MediatR;

namespace Accounting.Application.CashBankAccounts.Commands.Create;

public record CreateCashBankAccountCommand(
    CashBankAccountType Type,   // <-- enum (Cash|Bank)
    string Name,
    string? Iban
) : IRequest<CashBankAccountDetailDto>, ITransactionalRequest;
