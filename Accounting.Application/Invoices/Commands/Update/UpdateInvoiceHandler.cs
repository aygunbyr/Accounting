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

        // 2) Concurrency (RowVersion base64)
        _ctx.Entry(inv).Property(nameof(Invoice.RowVersion))
            .OriginalValue = Convert.FromBase64String(r.RowVersionBase64);

        // 3) Normalize (parent)
        inv.Currency = (r.Currency ?? "TRY").Trim().ToUpperInvariant();
        inv.DateUtc = r.DateUtc;
        inv.ContactId = r.ContactId;

        inv.Type = NormalizeType(r.Type, inv.Type);

        // ---- Satır diff senkronu ----
        var now = DateTime.UtcNow;

        // Snapshot doldurmak için gerekli Item'ları tek seferde çek
        var allItemIds = r.Lines.Select(x => x.ItemId).Distinct().ToList();
        var itemsMap = await _ctx.Items
            .Where(i => allItemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Code, i.Name, i.Unit, i.VatRate })
            .ToDictionaryAsync(i => i.Id, ct);

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
                var itemChanged = line.ItemId != dto.ItemId;
                line.ItemId = dto.ItemId;
                line.Qty = dto.Qty;
                line.UnitPrice = dto.UnitPrice;
                line.VatRate = dto.VatRate;
                line.UpdatedAtUtc = now;

                // Snapshot tazele (Item değişmişse ya da eksikse)
                if (itemsMap.TryGetValue(line.ItemId, out var it))
                {
                    if (itemChanged || string.IsNullOrWhiteSpace(line.ItemCode))
                    {
                        line.ItemCode = it.Code;
                        line.ItemName = it.Name;
                        line.Unit = it.Unit;
                    }
                }

                // Hesaplar (AwayFromZero politikası)
                line.Net = Money.R2(dto.Qty * dto.UnitPrice);
                line.Vat = Money.R2(line.Net * line.VatRate / 100m);
                var gross = Money.R2(net + vat);

                line.Net = Money.R2(net * sign);
                line.Vat = Money.R2(vat * sign);
                line.Gross = Money.R2(gross * sign);
            }
        }

        // c) Yeni satırlar
        foreach (var dto in r.Lines.Where(x => x.Id == 0))
        {
            // Snapshot: Item zorunlu
            if (!itemsMap.TryGetValue(dto.ItemId, out var it))
                throw new BusinessRuleException($"Item {dto.ItemId} bulunamadı.");

            var nl = new InvoiceLine
            {
                ItemId = dto.ItemId,
                ItemCode = it.Code,   // snapshot
                ItemName = it.Name,   // snapshot
                Unit = it.Unit,   // snapshot
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

        // 4) UpdatedAt + parent toplamlar (iade tiplerinde sign = -1)
        inv.UpdatedAtUtc = now;

        var lineNet = inv.Lines.Sum(x => x.Net);
        var lineVat = inv.Lines.Sum(x => x.Vat);
        var lineGross = inv.Lines.Sum(x => x.Gross);

        decimal sign = (inv.Type == InvoiceType.SalesReturn || inv.Type == InvoiceType.PurchaseReturn) ? -1m : 1m;

        inv.TotalNet = Money.R2(sign * lineNet);
        inv.TotalVat = Money.R2(sign * lineVat);
        inv.TotalGross = Money.R2(sign * lineGross);

        // 5) Save + concurrency
        try { await _ctx.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        { throw new ConcurrencyConflictException(); }

        // 6) Fresh read (AsNoTracking + Contact + Lines)
        var fresh = await _ctx.Invoices
            .AsNoTracking()
            .Include(i => i.Contact)
            .Include(i => i.Lines)
            .FirstAsync(i => i.Id == inv.Id, ct);

        // Lines → DTO (snapshot kullan)
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

        // 7) DTO build
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
            fresh.UpdatedAtUtc,
            (int)fresh.Type
        );
    }

    private static InvoiceType NormalizeType(string? incoming, InvoiceType fallback)
    {
        if (string.IsNullOrWhiteSpace(incoming)) return fallback;

        // "1" / "2" / "3" / "4"
        if (int.TryParse(incoming, out var n) && Enum.IsDefined(typeof(InvoiceType), n))
            return (InvoiceType)n;

        // "Sales" / "Purchase" / "SalesReturn" / "PurchaseReturn"
        return incoming.Trim().ToLowerInvariant() switch
        {
            "sales" => InvoiceType.Sales,
            "purchase" => InvoiceType.Purchase,
            "salesreturn" => InvoiceType.SalesReturn,
            "purchasereturn" => InvoiceType.PurchaseReturn,
            _ => fallback
        };
    }
}
