namespace Accounting.Application.Contacts.Queries.Dto;

public record ContactDto(
    int Id,
    string Name,
    string Type,        // "Customer" | "Vendor"
    string? Email,
    string RowVersion,  // base64
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record ContactListItemDto(
    int Id,
    string Name,
    string Type,
    string? Email,
    DateTime CreatedAtUtc
);

public record ContactListResult(
    int TotalCount,
    IReadOnlyList<ContactListItemDto> Items
);
