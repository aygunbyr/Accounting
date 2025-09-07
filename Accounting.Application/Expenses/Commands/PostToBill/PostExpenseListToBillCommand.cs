using Accounting.Application.Common.Abstractions;
using MediatR;

namespace Accounting.Application.Expenses.Commands.PostToBill;

public record PostExpenseListToBillCommand(
    int ExpenseListId,
    int SupplierId,
    string Currency,
    int ItemId,
    string? DateUtc = null    
    ) : IRequest<PostExpenseListToBillResult>, ITransactionalRequest; 

public record PostExpenseListToBillResult(
    int CreatedInvoiceId,
    int PostedExpenseCount
    );

