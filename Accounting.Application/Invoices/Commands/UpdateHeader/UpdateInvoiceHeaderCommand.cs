using Accounting.Application.Common.Abstractions;
using Accounting.Application.Invoices.Queries.Dto;
using Accounting.Domain.Enums;
using MediatR;

namespace Accounting.Application.Invoices.Commands.UpdateHeader;

public record UpdateInvoiceHeaderCommand(
    int Id,
    int ContactId,
    string DateUtc,
    string Currency,
    InvoiceType Type,
    string RowVersion // base64
    ) : IRequest<InvoiceDto>;
