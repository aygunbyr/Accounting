namespace Accounting.Application.Invoices.Queries.Dto;

public record InvoiceLineDto(
    int Id,
    int ItemId,
    string Qty,        // F3 string
    string UnitPrice,  // F4 string
    int VatRate,
    string Net,        // F2 string
    string Vat,        // F2 string
    string Gross       // F2 string
);

public record InvoiceDto(
    int Id,
    int ContactId,
    DateTime DateUtc,
    string Currency,
    string TotalNet,     // F2
    string TotalVat,     // F2
    string TotalGross,   // F2
    IReadOnlyList<InvoiceLineDto> Lines
);

public record InvoiceListItemDto(
    int Id,
    int ContactId,
    DateTime DateUtc,
    string Currency,
    string TotalNet,
    string TotalVat,
    string TotalGross
);
