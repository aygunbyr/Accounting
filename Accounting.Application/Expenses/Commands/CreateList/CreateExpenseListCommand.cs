using Accounting.Application.Common.Abstractions;
using Accounting.Application.Expenses.Queries.Dto;
using MediatR;

namespace Accounting.Application.Expenses.Commands.CreateList;

public record CreateExpenseListCommand(string? Name) 
    : IRequest<ExpenseListDto>, ITransactionalRequest;

