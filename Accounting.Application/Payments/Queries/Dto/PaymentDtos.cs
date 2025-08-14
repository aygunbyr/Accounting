namespace Accounting.Application.Payments.Queries.Dto;

public record PaymentListItemDto(
    int Id,
    int AccountId,
    int? ContactId,
    int? LinkedInvoiceId,
    DateTime DateUtc,
    string Direction,
    string Amount,
    string Currency
);
