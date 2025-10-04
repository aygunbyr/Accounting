using Accounting.Application.Common.Abstractions;
using Accounting.Application.Expenses.Queries.Dto;
using MediatR;

namespace Accounting.Application.Expenses.Commands.Update;

public record UpdateExpenseListNameCommand(
    int Id,
    string Name,
    string RowVersion // base64
) : IRequest<ExpenseListDetailDto>, ITransactionalRequest;
