using System.Globalization;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;   // ConcurrencyConflictException
using Accounting.Application.Common.Utils;    // Money.TryParse2 / Money.S2
using Accounting.Application.Payments.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class UpdatePaymentHandler : IRequestHandler<UpdatePaymentCommand, PaymentDetailDto>
{
    private readonly IAppDbContext _db;
    public UpdatePaymentHandler(IAppDbContext db) => _db = db;

    public async Task<PaymentDetailDto> Handle(UpdatePaymentCommand req, CancellationToken ct)
    {
        var p = await _db.Payments.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (p is null) throw new KeyNotFoundException($"Payment {req.Id} not found.");

        // concurrency
        var original = Convert.FromBase64String(req.RowVersion);
        _db.Entry(p).Property("RowVersion").OriginalValue = original;

        // parse & assign
        if (!DateTime.TryParse(req.DateUtc, CultureInfo.InvariantCulture,
                               DateTimeStyles.AdjustToUniversal, out var dt))
            throw new ArgumentException("DateUtc is invalid.");

        if (!Money.TryParse2(req.Amount, out var amount))
            throw new ArgumentException("Amount format is invalid.");

        p.AccountId = req.AccountId;
        p.ContactId = req.ContactId;
        p.LinkedInvoiceId = req.LinkedInvoiceId;
        p.DateUtc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        p.Direction = req.Direction;
        p.Amount = amount;
        p.Currency = req.Currency.ToUpperInvariant();

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Ödeme başka biri tarafından güncellendi.");
        }

        var inv = CultureInfo.InvariantCulture;

        return new PaymentDetailDto(
            p.Id,
            p.AccountId,
            p.ContactId,
            p.LinkedInvoiceId,
            p.DateUtc,
            p.Direction.ToString(),
            p.Amount.ToString("F2", inv),
            p.Currency,
            Convert.ToBase64String(p.RowVersion)
        );
    }
}
