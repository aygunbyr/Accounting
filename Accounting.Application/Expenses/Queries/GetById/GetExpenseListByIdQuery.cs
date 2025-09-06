using Accounting.Application.Expenses.Queries.Dto;
using MediatR;

namespace Accounting.Application.Expenses.Queries.GetById;

public record GetExpenseListByIdQuery(int Id) : IRequest<ExpenseListDetailDto>;
