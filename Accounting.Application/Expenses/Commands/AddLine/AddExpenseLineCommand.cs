using Accounting.Application.Common.Abstractions;
using Accounting.Application.Expenses.Queries.Dto;
using MediatR;

namespace Accounting.Application.Expenses.Commands.AddLine;

public record AddExpenseLineCommand(
    int ExpenseListId,
    string DateUtc,
    int? SupplierId,
    string Currency,
    string Amount,
    int VatRate,
    string? Category,
    string? Notes
    ) : IRequest<ExpenseLineDto>, ITransactionalRequest;
