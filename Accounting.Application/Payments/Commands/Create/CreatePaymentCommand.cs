using Accounting.Application.Common.Abstractions;
using Accounting.Domain.Entities;
using MediatR;

namespace Accounting.Application.Payments.Commands.Create;

public record CreatePaymentCommand(
    int AccountId,
    int? ContactId,
    int? LinkedInvoiceId,
    string DateUtc,
    PaymentDirection Direction,
    decimal Amount,
    string Currency
) : IRequest<CreatePaymentResult>, ITransactionalRequest;

public record CreatePaymentResult(
    int Id,
    string Amount,
    string Currency
);
