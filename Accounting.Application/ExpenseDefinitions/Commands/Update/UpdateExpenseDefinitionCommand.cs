using MediatR;

namespace Accounting.Application.ExpenseDefinitions.Commands.Update;

public record UpdateExpenseDefinitionCommand(
    int Id,
    string Code,
    string Name
) : IRequest;
