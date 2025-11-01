using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Application.Common.Utils;
using Accounting.Application.Invoices.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed class UpdateInvoiceHandler : IRequestHandler<UpdateInvoiceCommand, InvoiceDto>
{
    private readonly IAppDbContext _ctx;

    public UpdateInvoiceHandler(IAppDbContext ctx)
    { _ctx = ctx; }

    public async Task<InvoiceDto> Handle(UpdateInvoiceCommand r, CancellationToken ct)
    {
        // 1) Aggregate (+Include)
        var inv = await _ctx.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == r.Id, ct)
            ?? throw new NotFoundException(nameof(Invoice), r.Id);

        // 3) Concurrency (RowVersion base64)
        _ctx.Entry(inv).Property("RowVersion")
            .OriginalValue = Convert.FromBase64String(r.RowVersionBase64);

        // 4) Normalize
        inv.Currency = (r.Currency ?? "TRY").Trim().ToUpperInvariant();
        inv.DateUtc = r.DateUtc;
        inv.ContactId = r.ContactId;

        // ---- Satır diff senkronu ----
        var now = DateTime.UtcNow;
        var incomingById = r.Lines.Where(x => x.Id > 0).ToDictionary(x => x.Id);

        // a) Silinecekler: mevcutta var, body’de yok
        foreach (var line in inv.Lines.ToList())
        {
            if (!incomingById.ContainsKey(line.Id))
            {
                _ctx.InvoiceLines.Remove(line); // MVP: hard delete
            }
        }

        // b) Güncellenecekler
        foreach (var line in inv.Lines)
        {
            if (incomingById.TryGetValue(line.Id, out var dto))
            {
                line.ItemId = dto.ItemId;
                line.Qty = dto.Qty;
                line.UnitPrice = dto.UnitPrice;
                line.VatRate = dto.VatRate;
                line.UpdatedAtUtc = now;

                line.Net = Money.R2(dto.Qty * dto.UnitPrice);
                line.Vat = Money.R2(line.Net * line.VatRate / 100m);
                line.Gross = Money.R2(line.Net + line.Vat);
            }
        }

        // c) Yeni satırlar
        foreach (var dto in r.Lines.Where(x => x.Id == 0))
        {
            var nl = new InvoiceLine
            {
                ItemId = dto.ItemId,
                Qty = dto.Qty,
                UnitPrice = dto.UnitPrice,
                VatRate = dto.VatRate,
                CreatedAtUtc = now
            };
            nl.Net = Money.R2(dto.Qty * dto.UnitPrice);
            nl.Vat = Money.R2(nl.Net * nl.VatRate / 100m);
            nl.Gross = Money.R2(nl.Net + nl.Vat);

            inv.Lines.Add(nl);
        }

        // 5) UpdatedAt + parent toplamlar
        inv.UpdatedAtUtc = now;
        inv.TotalNet = Money.R2(inv.Lines.Sum(x => x.Net));
        inv.TotalVat = Money.R2(inv.Lines.Sum(x => x.Vat));
        inv.TotalGross = Money.R2(inv.Lines.Sum(x => x.Gross));

        // 6) Save + concurrency
        try { await _ctx.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        { throw new ConcurrencyConflictException(); }

        // 7) Fresh read (AsNoTracking)
        var fresh = await _ctx.Invoices
            .AsNoTracking()
            .Include(i => i.Contact)
            .Include(i => i.Lines)
            .FirstAsync(i => i.Id == inv.Id, ct);

        // Lines → DTO
        var linesDto = fresh.Lines
            .OrderBy(l => l.Id)
            .Select(l => new InvoiceLineDto(
                l.Id,
                l.ItemId,
                l.ItemCode,
                l.ItemName,
                l.Unit,
                Money.S3(l.Qty),
                Money.S4(l.UnitPrice),
                l.VatRate,
                Money.S2(l.Net),
                Money.S2(l.Vat),
                Money.S2(l.Gross)
            ))
            .ToList();

        // 8) DTO build (10 parametreli sürüm)
        return new InvoiceDto(
            fresh.Id,
            fresh.ContactId,
            fresh.Contact.Code,
            fresh.Contact.Name,
            fresh.DateUtc,
            fresh.Currency,
            Money.S2(fresh.TotalNet),
            Money.S2(fresh.TotalVat),
            Money.S2(fresh.TotalGross),
            linesDto,
            Convert.ToBase64String(fresh.RowVersion),
            fresh.CreatedAtUtc,
            fresh.UpdatedAtUtc
        );
    }
}
