namespace Accounting.Application.Reports.Queries;

public record StockStatusDto(
    int ItemId,
    string ItemCode,
    string ItemName,
    string Unit,
    string QuantityIn,       // Giren
    string QuantityOut,      // Çıkan
    string QuantityReserved, // Rezerve
    string QuantityAvailable // Mevcut
);
