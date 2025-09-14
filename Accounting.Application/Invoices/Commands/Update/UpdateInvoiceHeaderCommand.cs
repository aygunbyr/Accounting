using Accounting.Application.Common.Abstractions;
using Accounting.Application.Invoices.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;

namespace Accounting.Application.Invoices.Commands.Update;

public record UpdateInvoiceHeaderCommand(
    int Id,
    int ContactId,
    string DateUtc,
    string Currency,
    InvoiceType Type,
    string RowVersion // base64
    ) : IRequest<InvoiceDto>, ITransactionalRequest;
