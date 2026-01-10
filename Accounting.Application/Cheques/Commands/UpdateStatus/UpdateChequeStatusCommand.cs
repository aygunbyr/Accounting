using Accounting.Domain.Enums;
using MediatR;

namespace Accounting.Application.Cheques.Commands.UpdateStatus;

public record UpdateChequeStatusCommand(
    int Id,
    ChequeStatus NewStatus,
    DateTime? TransactionDate = null,
    int? CashBankAccountId = null // Tahsilat/Ödeme için gerekli
) : IRequest;
