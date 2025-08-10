namespace Accounting.Application.Invoices.Queries.Dto;

public record InvoiceLineDto(
    int ItemId,
    decimal Qty,
    decimal UnitPrice,
    int VatRate,
    decimal Net,
    decimal Vat,
    decimal Gross
);

public record InvoiceDto(
    int Id,
    int ContactId,
    DateTime DateUtc,
    string Currency,
    string TotalNet,
    string TotalVat,
    string TotalGross,
    List<InvoiceLineDto> Lines
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
