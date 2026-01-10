using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Extensions;
using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.ExpenseDefinitions.Commands.Create;

public class CreateExpenseDefinitionHandler : IRequestHandler<CreateExpenseDefinitionCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public CreateExpenseDefinitionHandler(IAppDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(CreateExpenseDefinitionCommand request, CancellationToken ct)
    {
        var branchId = _currentUserService.BranchId 
            ?? throw new UnauthorizedAccessException("User must have a branch assignment");

        // Check code uniqueness within branch
        var exists = await _db.ExpenseDefinitions
            .ApplyBranchFilter(_currentUserService)
            .AnyAsync(e => e.Code == request.Code, ct);
        
        if (exists)
            throw new FluentValidation.ValidationException("Code already exists in this branch");

        var expenseDef = new ExpenseDefinition
        {
            BranchId = branchId,
            Code = request.Code,
            Name = request.Name
        };

        _db.ExpenseDefinitions.Add(expenseDef);
        await _db.SaveChangesAsync(ct);

        return expenseDef.Id;
    }
}
