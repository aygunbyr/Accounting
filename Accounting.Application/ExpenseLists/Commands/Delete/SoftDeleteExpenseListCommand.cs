using Accounting.Application.Common.Abstractions;
using MediatR;

namespace Accounting.Application.ExpenseLists.Commands.Delete;

public record SoftDeleteExpenseListCommand(
    int Id,
    string RowVersion
) : IRequest;