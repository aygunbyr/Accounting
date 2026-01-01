using System.Globalization;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;   // ConcurrencyConflictException, BusinessRuleException
using Accounting.Application.Common.Utils;    // Money.TryParse2 / Money.S2
using Accounting.Application.Payments.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class UpdatePaymentHandler : IRequestHandler<UpdatePaymentCommand, PaymentDetailDto>
{
    private readonly IAppDbContext _db;
    public UpdatePaymentHandler(IAppDbContext db) => _db = db;

    public async Task<PaymentDetailDto> Handle(UpdatePaymentCommand req, CancellationToken ct)
    {
        // 1) Fetch (TRACKING)
        var p = await _db.Payments.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (p is null) throw new KeyNotFoundException($"Payment {req.Id} not found.");

        // 2) Business rules: (şimdilik yok)

        // 3) Concurrency
        byte[] rv;
        try { rv = Convert.FromBase64String(req.RowVersion); }
        catch { throw new ConcurrencyConflictException("RowVersion geçersiz."); }
        _db.Entry(p).Property(nameof(Payment.RowVersion)).OriginalValue = rv;

        // 4) Normalize/map
        if (!DateTime.TryParse(req.DateUtc, CultureInfo.InvariantCulture,
                               DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            throw new FluentValidation.ValidationException("DateUtc is invalid or not in UTC format.");

        if (!Money.TryParse2(req.Amount, out var amount))
            throw new BusinessRuleException("Amount format is invalid.");

        // Currency Normalization
        var currency = (req.Currency ?? "TRY").ToUpperInvariant();

        // Whitelist Validation
        var allowedCurrencies = new[] { "TRY", "USD", "EUR", "GBP" };
        if (!allowedCurrencies.Contains(currency))
            throw new FluentValidation.ValidationException($"Currency '{currency}' is not supported.");

        p.AccountId = req.AccountId;
        p.ContactId = req.ContactId;
        p.LinkedInvoiceId = req.LinkedInvoiceId;
        p.DateUtc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        p.Direction = req.Direction;
        p.Amount = amount; // decimal(18,2) — Money.TryParse2 + policy
        p.Currency = currency;

        // 5) Audit
        p.UpdatedAtUtc = DateTime.UtcNow;

        // 6) Persist
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        { throw new ConcurrencyConflictException("Ödeme başka biri tarafından güncellendi."); }

        // 7) Fresh read
        var fresh = await _db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == p.Id, ct);
        if (fresh is null) throw new KeyNotFoundException($"Payment {p.Id} not found after update.");

        // 8) DTO
        return new PaymentDetailDto(
            fresh.Id,
            fresh.AccountId,
            fresh.ContactId,
            fresh.LinkedInvoiceId,
            fresh.DateUtc,
            fresh.Direction.ToString(),
            Money.S2(fresh.Amount),
            fresh.Currency,
            Convert.ToBase64String(fresh.RowVersion),
            fresh.CreatedAtUtc,
            fresh.UpdatedAtUtc
        );
    }
}
