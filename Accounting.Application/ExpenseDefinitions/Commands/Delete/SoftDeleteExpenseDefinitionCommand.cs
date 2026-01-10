using MediatR;

namespace Accounting.Application.ExpenseDefinitions.Commands.Delete;

public record SoftDeleteExpenseDefinitionCommand(int Id) : IRequest;
