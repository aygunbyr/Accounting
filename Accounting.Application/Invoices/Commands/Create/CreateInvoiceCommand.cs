using Accounting.Application.Common.Abstractions;
using Accounting.Domain.Entities;
using MediatR;

namespace Accounting.Application.Invoices.Commands.Create;

public record CreateInvoiceCommand(
    int ContactId,
    string DateUtc,
    string Currency,
    List<CreateInvoiceLineDto> Lines,
    string Type // "Sales", "Purchase" vb.
) : IRequest<CreateInvoiceResult>, ITransactionalRequest;

public record CreateInvoiceResult(
    int Id,
    string TotalNet,
    string TotalVat,
    string TotalGross,
    string RoundingPolicy
);
