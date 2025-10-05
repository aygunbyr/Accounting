using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors; // BusinessRuleException, ConcurrencyConflictException
using Accounting.Application.Common.Utils;
using Accounting.Application.Expenses.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Accounting.Application.Expenses.Commands.Update;

public class UpdateExpenseListNameHandler
    : IRequestHandler<UpdateExpenseListNameCommand, ExpenseListDetailDto>
{
    private readonly IAppDbContext _db;
    public UpdateExpenseListNameHandler(IAppDbContext db) => _db = db;

    public async Task<ExpenseListDetailDto> Handle(UpdateExpenseListNameCommand req, CancellationToken ct)
    {
        // Header + Lines ile birlikte çek (TotalAmount hesaplayacağız)
        var list = await _db.ExpenseLists
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);

        if (list is null)
            throw new KeyNotFoundException($"ExpenseList {req.Id} not found.");

        // İş kuralı: Reviewed olan liste ismi değişmesin (istersen kaldır)
        if (list.Status == ExpenseListStatus.Reviewed)
            throw new BusinessRuleException("Onaylanmış masraf listesi güncellenemez.");

        // Concurrency
        var original = Convert.FromBase64String(req.RowVersion);
        _db.Entry(list).Property("RowVersion").OriginalValue = original;

        list.Name = req.Name.Trim();

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Masraf listesi başka biri tarafından güncellendi.");
        }

        // DTO üretimi (Amount'lar DB'de decimal ise F2 format; string ise mevcut değer)
        var inv = CultureInfo.InvariantCulture;

        var lines = list.Lines
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

        var total = list.Lines.Sum(l => l.Amount);
        var totalStr = Money.S2(total);

        return new ExpenseListDetailDto(
            list.Id,
            list.Name,
            list.CreatedAtUtc,
            list.Status.ToString(),
            lines,
            totalStr,
            Convert.ToBase64String(list.RowVersion),
            list.UpdatedAtUtc
        );
    }
}
