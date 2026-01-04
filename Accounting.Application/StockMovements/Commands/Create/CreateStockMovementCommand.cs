using Accounting.Application.StockMovements.Queries.Dto;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;

namespace Accounting.Application.StockMovements.Commands.Create;

public record CreateStockMovementCommand(
    int BranchId,
    int WarehouseId,
    int ItemId,
    StockMovementType Type,
    string Quantity,                 // FE string gelebilir
    DateTime? TransactionDateUtc,
    string? Note
) : IRequest<StockMovementDto>;
