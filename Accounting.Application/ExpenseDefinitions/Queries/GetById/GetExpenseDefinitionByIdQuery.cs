using Accounting.Application.ExpenseDefinitions.Queries.Dto;
using MediatR;

namespace Accounting.Application.ExpenseDefinitions.Queries.GetById;

public sealed record GetExpenseDefinitionByIdQuery(int Id)
    : IRequest<ExpenseDefinitionDetailDto>;