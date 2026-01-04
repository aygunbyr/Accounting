using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Application.Common.Utils;
using Accounting.Application.Invoices.Queries.Dto;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed class UpdateInvoiceHandler : IRequestHandler<UpdateInvoiceCommand, InvoiceDto>
{
    private readonly IAppDbContext _ctx;
    private readonly IInvoiceBalanceService _balanceService;

    public UpdateInvoiceHandler(IAppDbContext ctx, IInvoiceBalanceService balanceService)
    {
        _ctx = ctx;
        _balanceService = balanceService;
    }

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
        inv.BranchId = r.BranchId;
        inv.Type = NormalizeType(r.Type, inv.Type);

        // ---- Satır diff senkronu ----
        var now = DateTime.UtcNow;

        // sign: iade faturalarında -1, diğerlerinde +1
        decimal sign = (inv.Type == InvoiceType.SalesReturn || inv.Type == InvoiceType.PurchaseReturn) ? -1m : 1m;

        // Snapshot için gerekli Item'ları tek seferde çek
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

                // ✅ String → decimal parse
                if (!Money.TryParse4(dto.Qty, out var qty))
                    throw new BusinessRuleException($"Line {line.Id}: Invalid Qty format.");
                if (!Money.TryParse4(dto.UnitPrice, out var unitPrice))
                    throw new BusinessRuleException($"Line {line.Id}: Invalid UnitPrice format.");

                line.ItemId = dto.ItemId;
                line.Qty = Money.R3(qty * sign);           // ✅ parsed qty
                line.UnitPrice = Money.R4(unitPrice);      // ✅ parsed unitPrice
                line.VatRate = dto.VatRate;
                line.UpdatedAtUtc = now;

                // Snapshot tazele (Item değişmişse ya da eksikse)
                if (itemsMap.TryGetValue(line.ItemId, out var it) && (itemChanged || string.IsNullOrWhiteSpace(line.ItemCode)))
                {
                    line.ItemCode = it.Code;
                    line.ItemName = it.Name;
                    line.Unit = it.Unit;
                }

                // Hesaplar (AwayFromZero)
                var net = Money.R2(unitPrice * qty);       // ✅ parsed values
                var vat = Money.R2(net * line.VatRate / 100m);
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

            // ✅ String → decimal parse
            if (!Money.TryParse4(dto.Qty, out var qty))
                throw new BusinessRuleException($"New line: Invalid Qty format.");
            if (!Money.TryParse4(dto.UnitPrice, out var unitPrice))
                throw new BusinessRuleException($"New line: Invalid UnitPrice format.");

            var net = Money.R2(unitPrice * qty);        // ✅ parsed values
            var vat = Money.R2(net * dto.VatRate / 100m);
            var gross = Money.R2(net + vat);

            var nl = new InvoiceLine
            {
                ItemId = dto.ItemId,
                ItemCode = it.Code,
                ItemName = it.Name,
                Unit = it.Unit,
                Qty = Money.R3(qty * sign),             // ✅ parsed qty
                UnitPrice = Money.R4(unitPrice),        // ✅ parsed unitPrice
                VatRate = dto.VatRate,
                Net = Money.R2(net * sign),
                Vat = Money.R2(vat * sign),
                Gross = Money.R2(gross * sign),
                CreatedAtUtc = now
            };

            inv.Lines.Add(nl);
        }

        // 4) UpdatedAt + parent toplamlar (satırlar zaten işaretli)
        inv.UpdatedAtUtc = now;
        inv.TotalNet = Money.R2(inv.Lines.Sum(x => x.Net));
        inv.TotalVat = Money.R2(inv.Lines.Sum(x => x.Vat));
        inv.TotalGross = Money.R2(inv.Lines.Sum(x => x.Gross));

        // Toplamlar değişince bakiyeyi yeniden hesapla
        await _balanceService.RecalculateBalanceAsync(inv.Id, ct);

        // 5) Save + concurrency
        try { await _ctx.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        { throw new ConcurrencyConflictException(); }

        // 6) Fresh read (AsNoTracking + Contact + Lines)
        var fresh = await _ctx.Invoices
            .AsNoTracking()
            .Include(i => i.Branch)
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
            fresh.Contact?.Code ?? "",
            fresh.Contact?.Name ?? "",
            fresh.DateUtc,
            fresh.Currency,
            Money.S2(fresh.TotalNet),
            Money.S2(fresh.TotalVat),
            Money.S2(fresh.TotalGross),
            Money.S2(fresh.Balance),
            linesDto,
            Convert.ToBase64String(fresh.RowVersion),
            fresh.CreatedAtUtc,
            fresh.UpdatedAtUtc,
            (int)fresh.Type,
            fresh.BranchId,
            fresh.Branch.Code,
            fresh.Branch.Name
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