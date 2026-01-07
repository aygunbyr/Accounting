namespace Accounting.Application.StockMovements.Commands.Transfer;

public record StockTransferDto(
    bool Success,
    int OutMovementId,
    int InMovementId,
    string Message
);
