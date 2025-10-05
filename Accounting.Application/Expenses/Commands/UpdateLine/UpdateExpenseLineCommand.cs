using MediatR;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Expenses.Queries.Dto;

namespace Accounting.Application.Expenses.Commands.UpdateLine;

public record UpdateExpenseLineCommand(
    int LineId,
    int ExpenseListId,   // route/body içinde netleştiriyoruz
    string RowVersion,   // base64 (parent ExpenseList.RowVersion)
    DateTime DateUtc,
    int? SupplierId,
    string Currency,
    string Amount,       // F2 string
    int VatRate,
    string? Category,
    string? Notes
) : IRequest<ExpenseListDetailDto>, ITransactionalRequest;
