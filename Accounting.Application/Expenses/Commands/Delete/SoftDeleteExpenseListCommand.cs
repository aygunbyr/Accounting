using Accounting.Application.Common.Abstractions;
using MediatR;

namespace Accounting.Application.Expenses.Commands.Delete;

public record SoftDeleteExpenseListCommand(
    int Id,
    string RowVersion // base64
) : IRequest, ITransactionalRequest;
