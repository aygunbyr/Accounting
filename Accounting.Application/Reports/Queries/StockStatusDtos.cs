namespace Accounting.Application.Reports.Queries;

public record StockStatusDto(
    int ItemId,
    string ItemCode,
    string ItemName,
    string Unit,
    decimal QuantityIn,       // Giren
    decimal QuantityOut,      // Çıkan
    decimal QuantityReserved, // Rezerve
    decimal QuantityAvailable // Mevcut
);
