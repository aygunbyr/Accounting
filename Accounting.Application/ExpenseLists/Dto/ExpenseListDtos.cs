namespace Accounting.Application.ExpenseLists.Dto;

// List için basit DTO
public record ExpenseListDto(
    int Id,
    int BranchId,
    string Name,
    string Status,
    DateTime CreatedAtUtc
);

// Line DTO
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

// Detail DTO (Lines dahil)
public record ExpenseListDetailDto(
    int Id,
    int BranchId,
    string Name,
    string Status,
    IReadOnlyList<ExpenseLineDto> Lines,
    string TotalAmount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string RowVersion
);