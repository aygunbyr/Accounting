using Accounting.Application.Categories.Queries;
using Accounting.Application.Common.Abstractions;
using Accounting.Domain.Entities;
using MediatR;

namespace Accounting.Application.Categories.Commands.Create;

public record CreateCategoryCommand(
    string Name,
    string? Description,
    string? Color
) : IRequest<CategoryDto>;

public class CreateCategoryHandler(IAppDbContext db) : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand r, CancellationToken ct)
    {
        var category = new Category
        {
            Name = r.Name.Trim(),
            Description = r.Description?.Trim(),
            Color = r.Color?.Trim(),
            RowVersion = []
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.Color,
            Convert.ToBase64String(category.RowVersion),
            category.CreatedAtUtc,
            category.UpdatedAtUtc
        );
    }
}
