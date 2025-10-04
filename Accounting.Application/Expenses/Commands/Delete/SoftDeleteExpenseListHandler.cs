using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Expenses.Commands.Delete;

public class SoftDeleteExpenseListHandler : IRequestHandler<SoftDeleteExpenseListCommand>
{
    private readonly IAppDbContext _db;
    public SoftDeleteExpenseListHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SoftDeleteExpenseListCommand req, CancellationToken ct)
    {
        var list = await _db.ExpenseLists.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (list is null)
            throw new KeyNotFoundException($"ExpenseList {req.Id} not found.");

        // İş kuralı: Reviewed olan liste silinmesin
        if (list.Status == Domain.Entities.ExpenseListStatus.Reviewed)
            throw new BusinessRuleException("Onaylanmış masraf listesi silinemez.");

        var original = Convert.FromBase64String(req.RowVersion);
        _db.Entry(list).Property("RowVersion").OriginalValue = original;

        list.IsDeleted = true;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Masraf listesi başka biri tarafından güncellendi/silindi.");
        }
    }
}