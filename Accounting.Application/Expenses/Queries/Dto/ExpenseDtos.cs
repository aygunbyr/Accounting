namespace Accounting.Application.Expenses.Queries.Dto;

public record ExpenseListDto(
    int Id,
    string Name,
    DateTime CreatedUtc,
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
