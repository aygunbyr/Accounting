namespace Accounting.Application.Items.Queries.Dto;

public record ItemListItemDto(
    int Id,
    string Code,
    string Name,
    string Unit,
    int VatRate,
    string? DefaultUnitPrice,   // money string (S2) veya null
    DateTime CreatedAtUtc
);

public record ItemDetailDto(
    int Id,
    string Name,
    string Unit,
    int VatRate,
    string? DefaultUnitPrice,   // money string (S2) veya null
    string RowVersion,          // base64
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
