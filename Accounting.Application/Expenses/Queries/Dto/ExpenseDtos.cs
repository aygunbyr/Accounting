namespace Accounting.Application.Expenses.Queries.Dto;

public record ExpenseListDto(
    int Id,
    string Name,
    DateTime CreatedAtUtc,
    string Status
);

public record ExpenseLineDto(
    int Id,
    int ExpenseListId,
    DateTime DateUtc,
    int? SupplierId,
    string Currency,
    string Amount,
    int VatRate,
    string? Category,
    string? Notes
);

// Detay: CreatedAtUtc + UpdatedAtUtc + RowVersion
public record ExpenseListDetailDto(
    int Id,
    string Name,
    DateTime CreatedAtUtc,
    string Status,
    IReadOnlyList<ExpenseLineDto> Lines,
    string TotalAmount,
    string RowVersion,
    DateTime? UpdatedAtUtc
);
