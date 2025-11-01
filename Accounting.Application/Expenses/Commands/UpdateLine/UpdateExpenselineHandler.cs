using System.Globalization;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Application.Common.Utils;
using Accounting.Application.Expenses.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Expenses.Commands.UpdateLine;

public class UpdateExpenseLineHandler
    : IRequestHandler<UpdateExpenseLineCommand, ExpenseListDetailDto>
{
    private readonly IAppDbContext _db;
    public UpdateExpenseLineHandler(IAppDbContext db) => _db = db;

    public async Task<ExpenseListDetailDto> Handle(UpdateExpenseLineCommand req, CancellationToken ct)
    {
        // 1) Satırı ve parent'ı (tracking) al
        var line = await _db.Expenses
            .Include(x => x.ExpenseList) // parent yeterli, Lines'ı burada çekmiyoruz
            .FirstOrDefaultAsync(x => x.Id == req.LineId && x.ExpenseListId == req.ExpenseListId, ct);

        if (line is null)
            throw new KeyNotFoundException($"Expense line {req.LineId} not found.");

        var list = line.ExpenseList!;

        // 2) İş kuralı
        if (list.Status is not (ExpenseListStatus.Draft or ExpenseListStatus.Reviewed))
            throw new BusinessRuleException("Sadece Draft/Reviewed durumundaki masraf listesinde satır güncellenebilir.");

        // 3) Concurrency (parent RowVersion)
        byte[] rv;
        try { rv = Convert.FromBase64String(req.RowVersion); }
        catch { throw new ConcurrencyConflictException("RowVersion geçersiz."); }

        _db.Entry(list).Property(nameof(ExpenseList.RowVersion)).OriginalValue = rv;

        // 4) Normalize + ata
        if (!decimal.TryParse(req.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amountDec))
            throw new BusinessRuleException("Amount sayısal formatta olmalıdır.");

        amountDec = Money.R2(amountDec); // AwayFromZero, 2 hane

        var currency = (req.Currency ?? "TRY").Trim().ToUpperInvariant();
        if (currency.Length != 3)
            throw new BusinessRuleException("Currency 3 karakter olmalıdır (ISO 4217).");

        line.DateUtc = DateTime.SpecifyKind(req.DateUtc, DateTimeKind.Utc);
        line.SupplierId = req.SupplierId;
        line.Currency = currency;
        line.Amount = amountDec;
        line.VatRate = req.VatRate;
        line.Category = string.IsNullOrWhiteSpace(req.Category) ? null : req.Category.Trim();
        line.Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();
        line.UpdatedAtUtc = DateTime.UtcNow;

        list.UpdatedAtUtc = DateTime.UtcNow;

        // 5) Persist
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Masraf listesi başka biri tarafından güncellendi.");
        }

        // 6) Fresh read (deterministik DTO)
        var fresh = await _db.ExpenseLists
            .AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == list.Id, ct);

        if (fresh is null)
            throw new KeyNotFoundException($"ExpenseList {list.Id} not found after update.");

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
