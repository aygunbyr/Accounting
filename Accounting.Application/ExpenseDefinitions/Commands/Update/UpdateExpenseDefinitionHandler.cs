using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Extensions;
using Accounting.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.ExpenseDefinitions.Commands.Update;

public class UpdateExpenseDefinitionHandler : IRequestHandler<UpdateExpenseDefinitionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public UpdateExpenseDefinitionHandler(IAppDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateExpenseDefinitionCommand request, CancellationToken ct)
    {
        var expenseDef = await _db.ExpenseDefinitions
            .ApplyBranchFilter(_currentUserService)
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct);

        if (expenseDef is null)
            throw new NotFoundException("ExpenseDefinition", request.Id);

        // Check code uniqueness (excluding current)
        var exists = await _db.ExpenseDefinitions
            .ApplyBranchFilter(_currentUserService)
            .AnyAsync(e => e.Code == request.Code && e.Id != request.Id, ct);
        
        if (exists)
            throw new FluentValidation.ValidationException("Code already exists in this branch");

        expenseDef.Code = request.Code;
        expenseDef.Name = request.Name;

        await _db.SaveChangesAsync(ct);
    }
}
