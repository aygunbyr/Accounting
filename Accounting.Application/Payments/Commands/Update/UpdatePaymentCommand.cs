using Accounting.Application.Common.Abstractions;
using Accounting.Application.Payments.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;

public record UpdatePaymentCommand(
    int Id,
    int AccountId,
    int? ContactId,
    int? LinkedInvoiceId,
    string DateUtc,    // ISO-8601 UTC
    PaymentDirection Direction,
    string Amount,     // string money, örn "1250.00"
    string Currency,   // "TRY"
    string RowVersion  // base64
) : IRequest<PaymentDetailDto>, ITransactionalRequest;
