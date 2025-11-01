using Accounting.Application.Invoices.Queries.Dto;
using MediatR;

public sealed record UpdateInvoiceCommand(
    int Id,
    string RowVersionBase64,
    DateTime DateUtc,
    string Currency,
    int ContactId,
    IReadOnlyList<UpdateInvoiceLineDto> Lines
) : IRequest<InvoiceDto>; // dönüşte taze DTO

public sealed record UpdateInvoiceLineDto(
    int Id,          // 0 = new
    int ItemId,
    decimal Qty,
    decimal UnitPrice,
    int VatRate
);
