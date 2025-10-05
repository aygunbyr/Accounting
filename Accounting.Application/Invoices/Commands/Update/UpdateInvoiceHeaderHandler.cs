using System.Globalization;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;  // ConcurrencyConflictException
using Accounting.Application.Common.Utils;   // Money.*
using Accounting.Application.Invoices.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Invoices.Commands.Update;

public class UpdateInvoiceHeaderHandler
    : IRequestHandler<UpdateInvoiceHeaderCommand, InvoiceDto>
{
    private readonly IAppDbContext _db;
    public UpdateInvoiceHeaderHandler(IAppDbContext db) => _db = db;

    public async Task<InvoiceDto> Handle(UpdateInvoiceHeaderCommand req, CancellationToken ct)
    {
        // 1) Fetch (TRACKING)
        var inv = await _db.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == req.Id, ct);

        if (inv is null)
            throw new KeyNotFoundException($"Invoice {req.Id} not found.");

        // 2) Business rules: (şimdilik yok / domainine göre eklenebilir)

        // 3) Concurrency (parent RowVersion)
        byte[] rv;
        try { rv = Convert.FromBase64String(req.RowVersion); }
        catch { throw new ConcurrencyConflictException("RowVersion geçersiz."); }
        _db.Entry(inv).Property(nameof(Invoice.RowVersion)).OriginalValue = rv;

        // 4) Normalize/map
        inv.ContactId = req.ContactId;
        inv.Currency = (req.Currency ?? "TRY").ToUpperInvariant();
        inv.Type = req.Type;

        if (!DateTime.TryParse(req.DateUtc, CultureInfo.InvariantCulture,
                               DateTimeStyles.AdjustToUniversal, out var dt))
            throw new ArgumentException("DateUtc is invalid.");
        inv.DateUtc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        // 5) Audit
        inv.UpdatedAtUtc = DateTime.UtcNow;

        // 6) Persist
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        { throw new ConcurrencyConflictException("Fatura başka biri tarafından güncellendi."); }

        // 7) Fresh read
        var fresh = await _db.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == req.Id, ct);

        if (fresh is null)
            throw new KeyNotFoundException($"Invoice {req.Id} not found after update.");

        // 8) DTO
        var lines = fresh.Lines
            .OrderBy(l => l.Id)
            .Select(l => new InvoiceLineDto(
                l.Id,
                l.ItemId,
                Money.S3(l.Qty),
                Money.S4(l.UnitPrice),
                l.VatRate,
                Money.S2(l.Net),
                Money.S2(l.Vat),
                Money.S2(l.Gross)
            ))
            .ToList();

        return new InvoiceDto(
            fresh.Id,
            fresh.ContactId,
            fresh.DateUtc,
            fresh.Currency,
            Money.S2(fresh.TotalNet),
            Money.S2(fresh.TotalVat),
            Money.S2(fresh.TotalGross),
            lines,
            Convert.ToBase64String(fresh.RowVersion),
            fresh.CreatedAtUtc,
            fresh.UpdatedAtUtc
        );
    }
}
