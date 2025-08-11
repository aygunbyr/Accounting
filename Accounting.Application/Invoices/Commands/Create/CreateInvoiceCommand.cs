using Accounting.Application.Common.Abstractions;
using MediatR;

namespace Accounting.Application.Invoices.Commands.Create;

public record CreateInvoiceCommand(
    int ContactId,
    string DateUtc,
    string Currency,
    List<CreateInvoiceLineDto> Lines
) : IRequest<CreateInvoiceResult>, ITransactionalRequest;

public record CreateInvoiceLineDto(
    int ItemId,
    decimal Qty,
    decimal UnitPrice,
    int VatRate
);

public record CreateInvoiceResult(
    int Id,
    string TotalNet,
    string TotalVat,
    string TotalGross,
    string RoundingPolicy
);
