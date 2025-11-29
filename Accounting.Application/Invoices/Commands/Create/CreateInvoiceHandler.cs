using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Application.Common.Utils;                 // Money helper
using Accounting.Domain.Entities;                          // Invoice, InvoiceLine, InvoiceType
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Accounting.Application.Invoices.Commands.Create;

public class CreateInvoiceHandler
    : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResult>
{
    private readonly IAppDbContext _db;

    public CreateInvoiceHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<CreateInvoiceResult> Handle(CreateInvoiceCommand req, CancellationToken ct)
    {
        // 1) Tarihi ISO-8601 (UTC) olarak parse et
        if (!DateTime.TryParse(req.DateUtc, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal, out var dateUtc))
        {
            throw new ArgumentException("DateUtc is invalid.");
        }
        dateUtc = DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc);

        // 2) Currency normalize
        var currency = (req.Currency ?? "TRY").ToUpperInvariant();

        // 2.5) Type normalize (Update ile aynı mantık)           // NEW
        var invType = NormalizeType(req.Type, InvoiceType.Sales); // NEW

        // 3) sign: iade faturalarında -1, diğerlerinde +1
        decimal sign = (req.Type == InvoiceType.SalesReturn.ToString() || req.Type == InvoiceType.PurchaseReturn.ToString())
            ? -1m
            : 1m;

        // 4) Item snapshot (Code/Name/Unit)
        var itemIds = req.Lines.Select(l => l.ItemId).Distinct().ToList();

        var itemsMap = await _db.Items
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Code, i.Name, i.Unit, i.VatRate })
            .ToDictionaryAsync(i => i.Id, ct);

        // 5) Invoice entity oluştur (toplamlar sıfır)
        var invoice = new Invoice
        {
            BranchId = req.BranchId,
            ContactId = req.ContactId,
            DateUtc = dateUtc,
            Currency = currency,
            Type = invType,
            TotalNet = 0m,
            TotalVat = 0m,
            TotalGross = 0m,
            Lines = new List<InvoiceLine>()
        };

        // 6) Satırlar
        foreach (var line in req.Lines)
        {
            // Snapshot: Item zorunlu
            if (!itemsMap.TryGetValue(line.ItemId, out var it))
                throw new BusinessRuleException($"Item {line.ItemId} bulunamadı.");

            // Parse qty/unitPrice (string -> decimal)
            if (!decimal.TryParse(line.Qty, NumberStyles.Number, CultureInfo.InvariantCulture, out var qty))
                throw new ArgumentException("Qty is invalid.");

            if (!decimal.TryParse(line.UnitPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var unitPrice))
                throw new ArgumentException("UnitPrice is invalid.");

            // Kural: qty = 3 hane, unitPrice = 4 hane (AwayFromZero)
            qty = Money.R3(qty);
            unitPrice = Money.R4(unitPrice);

            // Net = qty * unitPrice (2 hane)
            var net = Money.R2(unitPrice * qty);

            // Vat = net * rate/100 (2 hane)
            var vat = Money.R2(net * line.VatRate / 100m);

            // Gross = net + vat (2 hane)
            var gross = Money.R2(net + vat);

            var lineEntity = new InvoiceLine
            {
                ItemId = line.ItemId,
                ItemCode = it.Code,   // 🔴 ÖNEMLİ: snapshot
                ItemName = it.Name,
                Unit = it.Unit,

                Qty = Money.R3(qty * sign),
                UnitPrice = unitPrice,
                VatRate = line.VatRate,

                Net = Money.R2(net * sign),
                Vat = Money.R2(vat * sign),
                Gross = Money.R2(gross * sign),
            };

            invoice.Lines.Add(lineEntity);

            invoice.TotalNet += lineEntity.Net;
            invoice.TotalVat += lineEntity.Vat;
            invoice.TotalGross += lineEntity.Gross;
        }

        // Her ihtimale karşı toplamları da policy ile son kez kapat (2 hane)
        invoice.TotalNet = Money.R2(invoice.TotalNet);
        invoice.TotalVat = Money.R2(invoice.TotalVat);
        invoice.TotalGross = Money.R2(invoice.TotalGross);

        // 5) Persist
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        // 6) Sonuç (response’ta string)
        return new CreateInvoiceResult(
            Id: invoice.Id,
            TotalNet: Money.S2(invoice.TotalNet),
            TotalVat: Money.S2(invoice.TotalVat),
            TotalGross: Money.S2(invoice.TotalGross),
            RoundingPolicy: "AwayFromZero"
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
