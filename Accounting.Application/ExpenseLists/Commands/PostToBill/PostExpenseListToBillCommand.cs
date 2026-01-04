using Accounting.Application.Common.Abstractions;
using MediatR;

namespace Accounting.Application.ExpenseLists.Commands.PostToBill;

public record PostExpenseListToBillCommand(
    int ExpenseListId,
    int SupplierId,
    int ItemId,
    string Currency,
    bool CreatePayment,
    int? PaymentAccountId = null,
    string? PaymentDateUtc = null,
    string? DateUtc = null
) : IRequest<PostExpenseListToBillResult>, ITransactionalRequest;

public record PostExpenseListToBillResult(
    int CreatedInvoiceId,
    int PostedExpenseCount
);