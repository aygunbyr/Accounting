namespace Accounting.Application.Contacts.Queries.Dto;

public record ContactDto(
    int Id,
    string Name,
    string Type, // "Customer" | "Vendor"
    string? Email,
    string RowVersion // base 64
    );

public record ContactListItemDto(
    int Id,
    string Name,
    string Type,
    string? Email
    );

public record ContactListResult(
    int TotalCount,
    IReadOnlyList<ContactListItemDto> Items
    );