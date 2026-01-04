using Accounting.Application.Common.Abstractions;
using MediatR;

namespace Accounting.Application.Expenses.Commands.PostToBill;

public record PostExpenseListToBillCommand(
    int ExpenseListId,
    int SupplierId,
    string Currency,
    int ItemId,
    bool CreatePayment,  // Otomatik ödeme oluştursun mu?
    int? PaymentAccountId = null,  // CreatePayment=true ise zorunlu
    string? PaymentDateUtc = null,
    string? DateUtc = null
    ) : IRequest<PostExpenseListToBillResult>, ITransactionalRequest;

public record PostExpenseListToBillResult(
    int CreatedInvoiceId,
    int PostedExpenseCount
    );

