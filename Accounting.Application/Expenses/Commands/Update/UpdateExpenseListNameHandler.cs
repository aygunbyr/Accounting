using System.Globalization;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors; // BusinessRuleException, ConcurrencyConflictException
using Accounting.Application.Common.Utils; // Money.*
using Accounting.Application.Expenses.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Expenses.Commands.Update;

public class UpdateExpenseListNameHandler
    : IRequestHandler<UpdateExpenseListNameCommand, ExpenseListDetailDto>
{
    private readonly IAppDbContext _db;
    public UpdateExpenseListNameHandler(IAppDbContext db) => _db = db;

    public async Task<ExpenseListDetailDto> Handle(UpdateExpenseListNameCommand req, CancellationToken ct)
    {
        // 1) Fetch (TRACKING)
        var list = await _db.ExpenseLists
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);

        if (list is null)
            throw new KeyNotFoundException($"ExpenseList {req.Id} not found.");

        // 2) Business rules
        if (list.Status == ExpenseListStatus.Reviewed)
            throw new BusinessRuleException("Onaylanmış masraf listesi güncellenemez.");

        // 3) Concurrency (parent RowVersion)
        byte[] rv;
        try { rv = Convert.FromBase64String(req.RowVersion); }
        catch { throw new ConcurrencyConflictException("RowVersion geçersiz."); }
        _db.Entry(list).Property(nameof(ExpenseList.RowVersion)).OriginalValue = rv;

        // 4) Normalize / map
        list.Name = req.Name.Trim();

        // 5) Audit
        list.UpdatedAtUtc = DateTime.UtcNow;

        // 6) Persist
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        { throw new ConcurrencyConflictException("Masraf listesi başka biri tarafından güncellendi."); }

        // 7) Fresh read
        var fresh = await _db.ExpenseLists
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == list.Id, ct);

        if (fresh is null)
            throw new KeyNotFoundException($"ExpenseList {list.Id} not found after update.");

        // 8) DTO
        var lineDtos = fresh.Lines
            .OrderBy(l => l.DateUtc)
            .Select(l => new ExpenseLineDto(
                l.Id,
                l.ExpenseListId,
                l.DateUtc,
                l.SupplierId,
                l.Currency,
                Money.S2(l.Amount),
                l.VatRate,
                l.Category,
                l.Notes
            ))
            .ToList();

        var total = fresh.Lines.Sum(l => l.Amount);

        return new ExpenseListDetailDto(
            fresh.Id,
            fresh.Name,
            fresh.CreatedAtUtc,
            fresh.Status.ToString(),
            lineDtos,
            Money.S2(total),
            Convert.ToBase64String(fresh.RowVersion),
            fresh.UpdatedAtUtc
        );
    }
}
