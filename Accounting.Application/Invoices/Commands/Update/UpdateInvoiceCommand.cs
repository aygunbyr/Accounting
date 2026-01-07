using Accounting.Application.Common.Abstractions;
using Accounting.Application.Invoices.Queries.Dto;
using MediatR;

public sealed record UpdateInvoiceCommand(
    int Id,
    string RowVersionBase64,
    DateTime DateUtc,
    string Currency,
    int ContactId,
    string Type,
    int BranchId,
    IReadOnlyList<UpdateInvoiceLineDto> Lines
) : IRequest<InvoiceDto>, ITransactionalRequest;

public sealed record UpdateInvoiceLineDto(
    int Id,          // 0 = new
    int? ItemId,
    int? ExpenseDefinitionId,
    string Qty,
    string UnitPrice,
    int VatRate
);
