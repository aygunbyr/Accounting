using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Extensions;
using Accounting.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.ExpenseDefinitions.Commands.Delete;

public class SoftDeleteExpenseDefinitionHandler : IRequestHandler<SoftDeleteExpenseDefinitionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public SoftDeleteExpenseDefinitionHandler(IAppDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SoftDeleteExpenseDefinitionCommand request, CancellationToken ct)
    {
        var expenseDef = await _db.ExpenseDefinitions
            .ApplyBranchFilter(_currentUserService)
            .FirstOrDefaultAsync(e => e.Id == request.Id && !e.IsDeleted, ct);

        if (expenseDef is null)
            throw new NotFoundException("ExpenseDefinition", request.Id);

        expenseDef.IsDeleted = true;
        expenseDef.DeletedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}
