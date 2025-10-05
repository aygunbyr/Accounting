using Accounting.Application.Expenses.Queries.Dto;
using MediatR;

namespace Accounting.Application.Expenses.Queries.GetLineById;

public record GetExpenseLineByIdQuery(int Id) : IRequest<ExpenseLineDto>;