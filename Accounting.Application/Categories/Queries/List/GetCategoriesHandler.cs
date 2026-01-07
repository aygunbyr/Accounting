using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Categories.Queries.List;

public record GetCategoriesQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<CategoryDto>>;

public class GetCategoriesHandler(IAppDbContext db) : IRequestHandler<GetCategoriesQuery, PagedResult<CategoryDto>>
{
    public async Task<PagedResult<CategoryDto>> Handle(GetCategoriesQuery r, CancellationToken ct)
    {
        var query = db.Categories
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            query = query.Where(x => x.Name.Contains(r.Search));
        }

        var totalCount = await query.CountAsync(ct);

        var categories = await query
            .OrderBy(x => x.Name)
            .Skip((r.Page - 1) * r.PageSize)
            .Take(r.PageSize)
            .ToListAsync(ct);

        var items = categories.Select(c => new CategoryDto(
            c.Id,
            c.Name,
            c.Description,
            c.Color,
            Convert.ToBase64String(c.RowVersion),
            c.CreatedAtUtc,
            c.UpdatedAtUtc
        )).ToList();

        return new PagedResult<CategoryDto>(totalCount, r.Page, r.PageSize, items);
    }
}
