﻿using System.Globalization;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Models;
using Accounting.Application.Common.Utils;
using Accounting.Application.Invoices.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Invoices.Queries.List;

public class ListInvoicesHandler : IRequestHandler<ListInvoicesQuery, PagedResult<InvoiceListItemDto>>
{
    private readonly IAppDbContext _db;
    public ListInvoicesHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<InvoiceListItemDto>> Handle(ListInvoicesQuery q, CancellationToken ct)
    {
        var query = _db.Invoices.AsNoTracking();

        // Filtreler
        if (q.ContactId is int cid) query = query.Where(i => i.ContactId == cid);

        // Type filtresi (varsa). Domain tarafında satış/alış ayırımını nasıl tuttuğuna göre uyarlayalım.
        // Örn: Invoice'ta bool IsPurchase gibi bir alan varsa:
        if (q.Type == InvoiceTypeFilter.Sales) query = query.Where(i => i.Type == InvoiceType.Sales);
        if (q.Type == InvoiceTypeFilter.Purchase) query = query.Where(i => i.Type == InvoiceType.Purchase);

        if (TryParseUtc(q.DateFromUtc, out var fromUtc)) query = query.Where(i => i.DateUtc >= fromUtc);
        if (TryParseUtc(q.DateToUtc, out var toUtc)) query = query.Where(i => i.DateUtc <= toUtc);

        // Sıralama
        var sort = (q.Sort ?? "dateUtc:desc").Split(':');
        var field = sort[0].ToLowerInvariant();
        var dir = sort.Length > 1 ? sort[1].ToLowerInvariant() : "desc";

        query = (field, dir) switch
        {
            ("totalgross", "asc") => query.OrderBy(i => i.TotalGross),
            ("totalgross", "desc") => query.OrderByDescending(i => i.TotalGross),
            ("dateutc", "asc") => query.OrderBy(i => i.DateUtc),
            _ => query.OrderByDescending(i => i.DateUtc),
        };

        // Toplam kayıt sayısı
        var total = await query.CountAsync(ct);

        // Filtered totals (DB tarafında SUM)
        var filteredNet = await query.Select(i => (decimal?)i.TotalNet).SumAsync(ct) ?? 0m;
        var filteredVat = await query.Select(i => (decimal?)i.TotalVat).SumAsync(ct) ?? 0m;
        var filteredGross = await query.Select(i => (decimal?)i.TotalGross).SumAsync(ct) ?? 0m;

        // Sayfa verisi
        var pageData = await query
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(i => new {
                i.Id,
                i.ContactId,
                i.DateUtc,
                i.Currency,
                i.TotalNet,
                i.TotalVat,
                i.TotalGross
            })
            .ToListAsync(ct);

        // Page totals (sayfadaki kalemlerin toplamı)
        var pageNet = pageData.Aggregate(0m, (acc, x) => acc + x.TotalNet);
        var pageVat = pageData.Aggregate(0m, (acc, x) => acc + x.TotalVat);
        var pageGross = pageData.Aggregate(0m, (acc, x) => acc + x.TotalGross);

        // Items
        var items = pageData.Select(i => new InvoiceListItemDto(
            i.Id,
            i.ContactId,
            i.DateUtc,
            i.Currency,
            Money.S2(i.TotalNet),
            Money.S2(i.TotalVat),
            Money.S2(i.TotalGross)
        )).ToList();

        var totals = new InvoicePagedTotals(
            PageTotalNet: Money.S2(pageNet),
            PageTotalVat: Money.S2(pageVat),
            PageTotalGross: Money.S2(pageGross),
            FilteredTotalNet: Money.S2(filteredNet),
            FilteredTotalVat: Money.S2(filteredVat),
            FilteredTotalGross: Money.S2(filteredGross)
        );

        return new PagedResult<InvoiceListItemDto>(total, q.PageNumber, q.PageSize, items, totals);
    }

    private static bool TryParseUtc(string? s, out DateTime value)
        => DateTime.TryParse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal, out value);
}
