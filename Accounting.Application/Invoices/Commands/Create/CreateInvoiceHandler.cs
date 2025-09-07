using System.Globalization;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Utils;                 // Money helper
using Accounting.Domain.Entities;                          // Invoice, InvoiceLine, InvoiceType
using MediatR;
using Microsoft.EntityFrameworkCore;

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

        // (Opsiyonel) ilgili Contact/Item var mı kontrol etmek istersen EF üzerinden doğrulayabilirsin.
        // Burada minimal gidiyoruz; doğrulama FluentValidation tarafında.

        // 3) Invoice entity oluştur (toplamlar sıfır)
        var invoice = new Invoice
        {
            ContactId = req.ContactId,
            DateUtc = dateUtc,
            Currency = currency,
            Type = req.Type,          // InvoiceType.Sales/Purchase
            TotalNet = 0m,
            TotalVat = 0m,
            TotalGross = 0m,
            Lines = new List<InvoiceLine>()
        };

        // 4) Satırları işle (string -> decimal parse + round; net/vat/gross hesapları)
        foreach (var line in req.Lines)
        {
            // Parse qty/unitPrice (string -> decimal)
            if (!decimal.TryParse(line.Qty, NumberStyles.Number, CultureInfo.InvariantCulture, out var qty))
                throw new ArgumentException("Qty is invalid.");

            if (!decimal.TryParse(line.UnitPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var unitPrice))
                throw new ArgumentException("UnitPrice is invalid.");

            // Kural: qty = 3 hane, unitPrice = 4 hane (AwayFromZero)
            qty = Money.R3(qty);
            unitPrice = Money.R4(unitPrice);

            // Net = qty * unitPrice (2 hane)
            var net = Money.R2(qty * unitPrice);

            // Vat = net * rate/100 (2 hane)
            var vat = Money.R2(net * line.VatRate / 100m);

            // Gross = net + vat (2 hane)
            var gross = net + vat;

            // Satır entity
            var lineEntity = new InvoiceLine
            {
                ItemId = line.ItemId,
                Qty = qty,
                UnitPrice = unitPrice,
                VatRate = line.VatRate,
                Net = net,
                Vat = vat,
                Gross = gross
            };

            invoice.Lines.Add(lineEntity);

            // Fatura toplamlarına ekle
            invoice.TotalNet += net;
            invoice.TotalVat += vat;
            invoice.TotalGross += gross;
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
}
