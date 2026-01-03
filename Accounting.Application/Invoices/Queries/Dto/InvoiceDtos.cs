namespace Accounting.Application.Invoices.Queries.Dto;

public record InvoiceLineDto(
    int Id,
    int ItemId,
    string ItemCode,
    string ItemName,
    string Unit,
    string Qty,        // F3
    string UnitPrice,  // F4
    int VatRate,
    string Net,        // F2
    string Vat,        // F2
    string Gross       // F2
);

public record InvoiceDto(
    int Id,
    int ContactId,
    string ContactCode,
    string ContactName,
    DateTime DateUtc,        // Belge tarihi (iş mantığı)
    string Currency,
    string TotalNet,         // F2
    string TotalVat,         // F2
    string TotalGross,       // F2
    string Balance,
    IReadOnlyList<InvoiceLineDto> Lines,
    string RowVersion,       // base64
    DateTime CreatedAtUtc,   // Audit
    DateTime? UpdatedAtUtc,   // Audit
    int Type,
    int BranchId,
    string BranchCode,
    string BranchName
);

public record InvoiceListItemDto(
    int Id,
    int ContactId,
    string ContactCode,
    string ContactName,
    string Type, // Sales / Purchase
    DateTime DateUtc,
    string Currency,
    string TotalNet,
    string TotalVat,
    string TotalGross,
    string Balance,
    DateTime CreatedAtUtc,
    int BranchId,
    string BranchCode,
    string BranchName
);
