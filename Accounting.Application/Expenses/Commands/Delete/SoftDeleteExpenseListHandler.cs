using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors; // BusinessRuleException, ConcurrencyConflictException
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
        // Load parent + children with tracking
        var list = await _db.ExpenseLists
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);

        if (list is null)
            throw new KeyNotFoundException($"ExpenseList {req.Id} not found.");

        // Concurrency: parse + set original
        byte[] originalRv;
        try
        {
            originalRv = Convert.FromBase64String(req.RowVersion);
        }
        catch
        {
            throw new ConcurrencyConflictException("RowVersion is invalid.");
        }

        _db.Entry(list)
            .Property(nameof(ExpenseList.RowVersion))
            .OriginalValue = originalRv;

        // Business rules
        if (list.Status == ExpenseListStatus.Posted)
            throw new BusinessRuleException("Posted (faturalandırılmış) masraf listesi silinemez.");

        // (İstersen bunu da korursun)
        if (list.Status == ExpenseListStatus.Reviewed)
            throw new BusinessRuleException("Onaylanmış masraf listesi silinemez.");

        // Parent soft delete + audit
        var now = DateTime.UtcNow;
        list.IsDeleted = true;
        list.DeletedAtUtc = now;
        list.UpdatedAtUtc = now;

        // Children soft delete + audit
        foreach (var line in list.Lines)
        {
            line.IsDeleted = true;
            line.DeletedAtUtc = now;
            line.UpdatedAtUtc = now;
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
