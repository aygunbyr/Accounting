namespace Accounting.Application.Categories.Queries;

public record CategoryDto(
    int Id,
    string Name,
    string? Description,
    string? Color,
    string RowVersion,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
