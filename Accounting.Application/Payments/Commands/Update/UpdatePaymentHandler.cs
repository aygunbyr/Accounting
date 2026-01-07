using System.Globalization;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;   // ConcurrencyConflictException, BusinessRuleException
using Accounting.Application.Common.Utils;    // Money.TryParse2 / Money.S2
using Accounting.Application.Payments.Queries.Dto;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class UpdatePaymentHandler : IRequestHandler<UpdatePaymentCommand, PaymentDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly IInvoiceBalanceService _balanceService;

    public UpdatePaymentHandler(IAppDbContext db, IInvoiceBalanceService balanceService)
    {
        _db = db;
        _balanceService = balanceService;
    }

    public async Task<PaymentDetailDto> Handle(UpdatePaymentCommand req, CancellationToken ct)
    {
        // 1) Fetch (TRACKING)
        var p = await _db.Payments.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (p is null) throw new NotFoundException("Payment", req.Id);

        // Eski LinkedInvoiceId'yi sakla (balance recalc için)
        var oldLinkedInvoiceId = p.LinkedInvoiceId;

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

        // 7) Recalculate balances (eski ve yeni invoice için)
        var invoicesToRecalc = new HashSet<int>();
        if (oldLinkedInvoiceId.HasValue) invoicesToRecalc.Add(oldLinkedInvoiceId.Value);
        if (p.LinkedInvoiceId.HasValue) invoicesToRecalc.Add(p.LinkedInvoiceId.Value);

        foreach (var invoiceId in invoicesToRecalc)
        {
            await _balanceService.RecalculateBalanceAsync(invoiceId, ct);
        }
        if (invoicesToRecalc.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        // 8) Fresh read
        var fresh = await _db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == p.Id, ct);
        if (fresh is null) throw new NotFoundException("Payment", req.Id);

        // 9) DTO
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
