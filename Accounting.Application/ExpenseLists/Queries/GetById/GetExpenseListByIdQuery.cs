using Accounting.Application.ExpenseLists.Dto;
using MediatR;

namespace Accounting.Application.ExpenseLists.Queries.GetById;

public record GetExpenseListByIdQuery(int Id) : IRequest<ExpenseListDetailDto>;