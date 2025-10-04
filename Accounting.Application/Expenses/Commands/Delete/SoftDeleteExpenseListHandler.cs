using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Expenses.Commands.Delete;

public class SoftDeleteExpenseListHandler : IRequestHandler<SoftDeleteExpenseListCommand>
{
    private readonly IAppDbContext _db;
    public SoftDeleteExpenseListHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SoftDeleteExpenseListCommand req, CancellationToken ct)
    {
        var list = await _db.ExpenseLists
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);

        if (list is null)
            throw new KeyNotFoundException($"ExpenseList {req.Id} not found.");

        // concurrency
        var original = Convert.FromBase64String(req.RowVersion);
        _db.Entry(list).Property("RowVersion").OriginalValue = original;

        // iş kuralı (opsiyonel): Reviewed olan silinemez
        if (list.Status == ExpenseListStatus.Reviewed)
            throw new BusinessRuleException("Onaylanmış masraf listesi silinemez.");

        // parent soft delete
        list.IsDeleted = true;
        list.DeletedAtUtc = DateTime.UtcNow;

        // children soft delete
        foreach (var line in list.Lines)
        {
            line.IsDeleted = true;
            line.DeletedAtUtc = DateTime.UtcNow;
        }

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
