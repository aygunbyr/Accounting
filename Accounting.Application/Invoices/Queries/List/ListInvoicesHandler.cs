using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Models;
using Accounting.Application.Invoices.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Accounting.Application.Invoices.Queries.List;

public class ListInvoicesHandler : IRequestHandler<ListInvoicesQuery, PagedResult<InvoiceListItemDto>>
{
    private readonly IAppDbContext _db;
    public ListInvoicesHandler(IAppDbContext db) => _db = db;

    public async Task<PagedResult<InvoiceListItemDto>> Handle(ListInvoicesQuery q, CancellationToken cancellationToken)
    {
        var query = _db.Invoices.AsNoTracking();

        var sort = (q.Sort ?? "dateUtc:desc").Split(":");

        var field = sort[0].ToLowerInvariant();
        var dir = sort.Length > 1 ? sort[1].ToLowerInvariant() : "desc";

        query = (field, dir) switch
        {
            ("totalnet", "asc") => query.OrderBy(i => i.TotalNet),
            ("totalnet", "desc") => query.OrderByDescending(i => i.TotalNet),
            ("totalgross", "asc") => query.OrderBy(i => i.TotalGross),
            ("totalgross", "desc") => query.OrderByDescending(i => i.TotalGross),
            ("dateutc", "asc") => query.OrderBy(i => i.DateUtc),
            _ => query.OrderByDescending(i => i.DateUtc)
        };

        var total = await query.CountAsync(cancellationToken);

        var itemsRaw = await query
           .Skip((q.PageNumber - 1) * q.PageSize)
           .Take(q.PageSize)
           .Select(i => new { i.Id, i.ContactId, i.DateUtc, i.Currency, i.TotalNet, i.TotalVat, i.TotalGross })
           .ToListAsync(cancellationToken);

        var invC = CultureInfo.InvariantCulture;

        var items = itemsRaw.Select(i => new InvoiceListItemDto(
            i.Id, i.ContactId, i.DateUtc, i.Currency,
            i.TotalNet.ToString("F2", invC),
            i.TotalVat.ToString("F2", invC),
            i.TotalGross.ToString("F2", invC)
        )).ToList();

        return new PagedResult<InvoiceListItemDto>(total, q.PageNumber, q.PageSize, items);
    }
}
