using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Models;
using Accounting.Application.Common.Utils;
using Accounting.Application.Expenses.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Accounting.Application.Expenses.Queries.List;

public class ListExpensesHandler : IRequestHandler<ListExpensesQuery, PagedResult<ExpenseLineDto>>
{
    private readonly IAppDbContext _db;
    public ListExpensesHandler(IAppDbContext db) => _db = db;
    public async Task<PagedResult<ExpenseLineDto>> Handle(ListExpensesQuery q, CancellationToken ct)
    {
        var query = _db.Expenses.AsNoTracking();

        // --- Filtreler ---
        if (q.ExpenseListId is int listId) query = query.Where(x => x.ExpenseListId == listId);
        if (q.SupplierId is int supId) query = query.Where(x => x.SupplierId == supId);
        if (!string.IsNullOrWhiteSpace(q.Currency))
        {
            var cur = q.Currency!.ToUpperInvariant();
            query = query.Where(x => x.Currency == cur);
        }
        if (!string.IsNullOrWhiteSpace(q.Category))
        {
            var cat = q.Category!.Trim();
            query = query.Where(x => x.Category != null && x.Category == cat);
        }
        if (TryParseUtc(q.DateFromUtc, out var fromUtc)) query = query.Where(x => x.DateUtc >= fromUtc);
        if (TryParseUtc(q.DateToUtc, out var toUtc)) query = query.Where(x => x.DateUtc <= toUtc);

        // --- Sıralama ---
        var sort = (q.Sort ?? "dateUtc:desc").Split(':');
        var field = sort[0].ToLowerInvariant();
        var dir = sort.Length > 1 ? sort[1].ToLowerInvariant() : "desc";

        query = (field, dir) switch
        {
            ("amount", "asc") => query.OrderBy(x => x.Amount),
            ("amount", "desc") => query.OrderByDescending(x => x.Amount),
            ("dateutc", "asc") => query.OrderBy(x => x.DateUtc),
            _ => query.OrderByDescending(x => x.DateUtc),
        };

        // --- Toplam kayıt sayısı ---
        var total = await query.CountAsync(ct);

        // --- Filtered toplam (sayfa sınırı yok) ---
        var filteredSum = await query.Select(x => (decimal?)x.Amount).SumAsync(ct) ?? 0m;

        // --- Sayfa verisi ---
        var pageData = await query
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(x => new {
                x.Id,
                x.ExpenseListId,
                x.DateUtc,
                x.SupplierId,
                x.Currency,
                x.Amount,
                x.VatRate,
                x.Category,
                x.Notes
            })
            .ToListAsync(ct);

        // --- Page toplamı ---
        var pageSum = pageData.Aggregate(0m, (acc, x) => acc + x.Amount);

        // --- Items ---
        var items = pageData.Select(x => new ExpenseLineDto(
            x.Id,
            x.ExpenseListId,
            x.DateUtc,
            x.SupplierId,
            x.Currency,
            Money.S2(x.Amount),
            x.VatRate,
            x.Category,
            x.Notes
        )).ToList();

        var totals = new PagedTotals(
            PageTotalAmount: Money.S2(pageSum),
            FilteredTotalAmount: Money.S2(filteredSum)
        );

        return new PagedResult<ExpenseLineDto>(total, q.PageNumber, q.PageSize, items, totals);

    }

    private static bool TryParseUtc(string? s, out DateTime value)
        => DateTime.TryParse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal, out value);
}
