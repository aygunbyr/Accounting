using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Utils;
using Accounting.Domain.Entities;
using MediatR;
using System.Globalization;

namespace Accounting.Application.Payments.Commands.Create;

public class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, CreatePaymentResult>
{
    private readonly IAppDbContext _db;
    public CreatePaymentHandler(IAppDbContext db) => _db = db;

    public async Task<CreatePaymentResult> Handle(CreatePaymentCommand req, CancellationToken ct)
    {
        if (!DateTime.TryParse(req.DateUtc, CultureInfo.InvariantCulture,
                               DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                               out var parsed))
            throw new FluentValidation.ValidationException("DateUtc is invalid or not in UTC format.");

        // Amount Parse
        if (!Money.TryParse2(req.Amount, out var amount))
            throw new FluentValidation.ValidationException("Amount is invalid.");

        // Currency Normalization
        var currency = (req.Currency ?? "TRY").ToUpperInvariant();

        // Whitelist Validation
        var allowedCurrencies = new[] { "TRY", "USD", "EUR", "GBP" };
        if (!allowedCurrencies.Contains(currency))
            throw new FluentValidation.ValidationException($"Currency '{currency}' is not supported.");

        var entity = new Payment
        {
            AccountId = req.AccountId,
            ContactId = req.ContactId,
            LinkedInvoiceId = req.LinkedInvoiceId,
            DateUtc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc),
            Direction = req.Direction,
            Amount = amount,  // decimal(18,2) DB
            Currency = currency
        };

        _db.Payments.Add(entity);
        await _db.SaveChangesAsync(ct);

        var inv = CultureInfo.InvariantCulture;
        return new CreatePaymentResult(
            entity.Id,
            Money.S2(entity.Amount),
            entity.Currency
        );
    }
}
