using Accounting.Application.Common.Abstractions;
using Accounting.Application.ExpenseLists.Dto;
using MediatR;

namespace Accounting.Application.ExpenseLists.Commands.Create;

public record CreateExpenseListCommand(
    int BranchId,
    string? Name,
    List<CreateExpenseLineDto> Lines
) : IRequest<ExpenseListDetailDto>, ITransactionalRequest;

public record CreateExpenseLineDto(
    string DateUtc,
    int? SupplierId,
    string Currency,
    string Amount,
    int VatRate,
    string? Category,
    string? Notes
);