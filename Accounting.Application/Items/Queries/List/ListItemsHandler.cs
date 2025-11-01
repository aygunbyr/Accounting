using Accounting.Application.Common.Models;
using Accounting.Application.Common.Utils;
using Accounting.Application.Items.Queries.Dto;
using Accounting.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Items.Queries.List;

public class ListItemsHandler(IAppDbContext db) : IRequestHandler<ListItemsQuery, PagedResult<ItemListItemDto>>
{
    public async Task<PagedResult<ItemListItemDto>> Handle(ListItemsQuery r, CancellationToken ct)
    {
        var q = db.Items.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var s = r.Search.Trim().ToUpperInvariant();
            q = q.Where(x => EF.Functions.Like(x.Name.ToUpper(), $"%{s}%"));
        }

        if (!string.IsNullOrWhiteSpace(r.Unit))
        {
            var u = r.Unit.Trim().ToLowerInvariant();
            q = q.Where(x => x.Unit.ToLower() == u);
        }

        if (r.VatRate is not null)
            q = q.Where(x => x.VatRate == r.VatRate);

        q = (r.Sort?.ToLowerInvariant()) switch
        {
            "code:asc" => q.OrderBy(x => x.Code),
            "code:desc" => q.OrderByDescending(x => x.Code),
            "name:asc" => q.OrderBy(x => x.Name),
            "name:desc" => q.OrderByDescending(x => x.Name),
            "vatrate:asc" => q.OrderBy(x => x.VatRate).ThenBy(x => x.Name),
            "vatrate:desc" => q.OrderByDescending(x => x.VatRate).ThenBy(x => x.Name),
            "price:asc" => q.OrderBy(x => x.DefaultUnitPrice ?? 0).ThenBy(x => x.Name),
            "price:desc" => q.OrderByDescending(x => x.DefaultUnitPrice ?? 0).ThenBy(x => x.Name),
            _ => q.OrderBy(x => x.Name)
        };

        var total = await q.CountAsync(ct);

        var items = await q.Skip((r.PageNumber - 1) * r.PageSize)
                           .Take(r.PageSize)
                           .Select(x => new ItemListItemDto(
                               x.Id, x.Code, x.Name, x.Unit, x.VatRate,
                               x.DefaultUnitPrice == null ? null : Money.S2(x.DefaultUnitPrice.Value),
                               x.CreatedAtUtc))
                           .ToListAsync(ct);

        return new PagedResult<ItemListItemDto>(
            total,
            r.PageNumber,
            r.PageSize,
            items,
            null // Totals
        );
    }
}
