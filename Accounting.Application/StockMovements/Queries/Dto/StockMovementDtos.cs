using Accounting.Domain.Enums;

namespace Accounting.Application.StockMovements.Queries.Dto;

public record StockMovementDto(
    int Id,
    int BranchId,
    int WarehouseId,
    string WarehouseCode,
    int ItemId,
    string ItemCode,
    string ItemName,
    string Unit,
    StockMovementType Type,
    string Quantity,
    DateTime TransactionDateUtc,
    string? Note,
    string RowVersion,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);
