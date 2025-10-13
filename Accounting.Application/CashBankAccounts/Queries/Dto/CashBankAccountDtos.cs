namespace Accounting.Application.CashBankAccounts.Queries.Dto;

public record CashBankAccountListItemDto(
    int Id,
    string Type,        // "Cash" | "Bank"
    string Name,
    string? Iban,
    DateTime CreatedAtUtc
);

public record CashBankAccountDetailDto(
    int Id,
    string Type,
    string Name,
    string? Iban,
    string RowVersion,      // base64
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
