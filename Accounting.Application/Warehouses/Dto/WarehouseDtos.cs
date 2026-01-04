namespace Accounting.Application.Warehouses.Dto;

public record WarehouseDto(
    int Id,
    int BranchId,
    string Code,
    string Name,
    bool IsDefault,
    string RowVersion,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
