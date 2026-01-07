using Accounting.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Categories.Queries.List;

public record GetCategoriesQuery() : IRequest<List<CategoryDto>>;

public class GetCategoriesHandler(IAppDbContext db) : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(c => new CategoryDto(
            c.Id,
            c.Name,
            c.Description,
            c.Color,
            Convert.ToBase64String(c.RowVersion),
            c.CreatedAtUtc,
            c.UpdatedAtUtc
        )).ToList();
    }
}
