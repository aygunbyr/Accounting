using MediatR;

namespace Accounting.Application.ExpenseDefinitions.Commands.Create;

public record CreateExpenseDefinitionCommand(
    string Code,
    string Name
) : IRequest<int>;
