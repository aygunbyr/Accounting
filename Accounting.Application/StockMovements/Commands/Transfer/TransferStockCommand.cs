using MediatR;

namespace Accounting.Application.StockMovements.Commands.Transfer;

public record TransferStockCommand(
    int SourceWarehouseId,
    int TargetWarehouseId,
    int ItemId,
    string Quantity, // String to avoid decimal issues in API JSON
    DateTime TransactionDateUtc,
    string? Description
) : IRequest<StockTransferDto>;
