using Accounting.Application.Common.Abstractions;
using Accounting.Application.Expenses.Queries.Dto;
using MediatR;

namespace Accounting.Application.Expenses.Commands.Review;

public record ReviewExpenseListCommand(int Id)
    : IRequest<ExpenseListDto>, ITransactionalRequest;