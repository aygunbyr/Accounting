using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;

namespace Accounting.Application.Payments.Commands.Create;

public record CreatePaymentCommand(
    int BranchId,
    int AccountId,
    int? ContactId,
    int? LinkedInvoiceId,
    string DateUtc,
    PaymentDirection Direction,
    string Amount,
    string Currency
) : IRequest<CreatePaymentResult>;

public record CreatePaymentResult(
    int Id,
    string Amount,
    string Currency
);
