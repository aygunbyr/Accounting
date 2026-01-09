using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Application.Common.Utils;                 // Money helper
using Accounting.Domain.Entities;                          // Invoice, InvoiceLine, InvoiceType
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Accounting.Application.Invoices.Commands.Create;

public class CreateInvoiceHandler
    : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResult>
{
    private readonly IAppDbContext _db;
    private readonly IMediator _mediator;
    private readonly IStockService _stockService;

    public CreateInvoiceHandler(IAppDbContext db, IMediator mediator, IStockService stockService)
    {
        _db = db;
        _mediator = mediator;
        _stockService = stockService;
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

        // 3) sign logic removed (All positive)

        // 4) Item veya Expense snapshot (Code/Name/Unit)
        // Hangi tip fatura kesiyoruz?
        Dictionary<int, dynamic>? itemsMap = null;
        Dictionary<int, dynamic>? expensesMap = null;

        if (invType == InvoiceType.Expense)
        {
             var expenseIds = req.Lines
                .Where(x => x.ExpenseDefinitionId.HasValue)
                .Select(l => l.ExpenseDefinitionId!.Value)
                .Distinct()
                .ToList();
            
             expensesMap = await _db.ExpenseDefinitions
                .Where(i => expenseIds.Contains(i.Id))
                .Select(i => new { i.Id, i.Code, i.Name })
                .ToDictionaryAsync(i => i.Id, i => (dynamic)i, ct);
        }
        else
        {
             var itemIds = req.Lines
                .Where(x => x.ItemId.HasValue)
                .Select(l => l.ItemId!.Value)
                .Distinct()
                .ToList();

             // STOCK VALIDATION (Only for Sales)
             if (invType == InvoiceType.Sales)
             {
                 foreach (var line in req.Lines)
                 {
                     if (line.ItemId.HasValue && decimal.TryParse(line.Qty, NumberStyles.Number, CultureInfo.InvariantCulture, out var qty))
                     {
                         var absQty = Math.Abs(qty);
                         if (absQty > 0)
                         {
                             await _stockService.ValidateStockAvailabilityAsync(line.ItemId.Value, absQty, ct);
                         }
                     }
                 }
             }

             itemsMap = await _db.Items
                .Where(i => itemIds.Contains(i.Id))
                .Select(i => new { i.Id, i.Code, i.Name, i.Unit, i.VatRate })
                .ToDictionaryAsync(i => i.Id, i => (dynamic)i, ct);
        }

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
            // Parse qty/unitPrice (string -> decimal)
            if (!decimal.TryParse(line.Qty, NumberStyles.Number, CultureInfo.InvariantCulture, out var qty))
                throw new ArgumentException("Qty is invalid.");

            if (!decimal.TryParse(line.UnitPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var unitPrice))
                throw new ArgumentException("UnitPrice is invalid.");

            // Kural: qty = 3 hane, unitPrice = 4 hane (AwayFromZero)
            qty = Money.R3(qty);
            unitPrice = Money.R4(unitPrice);
            
            // DB'ye her zaman pozitif Qty kaydediyoruz
            var absQty = Math.Abs(qty);
            
            // Net = qty * unitPrice (2 hane)
            var net = Money.R2(unitPrice * absQty);

            // Vat = net * rate/100 (2 hane)
            var vat = Money.R2(net * line.VatRate / 100m);

            // Gross = net + vat (2 hane)
            var gross = Money.R2(net + vat);

            var lineEntity = new InvoiceLine
            {
                Qty = absQty, // DB: Positive per constraint
                UnitPrice = unitPrice,
                VatRate = line.VatRate,
                Net = Money.R2(net),
                Vat = Money.R2(vat),
                Gross = Money.R2(gross),
            };

            if (invType == InvoiceType.Expense)
            {
                // Masraf Faturası: ItemId yasak, ExpenseDefinitionId zorunlu
                if (line.ItemId.HasValue)
                    throw new BusinessRuleException("Masraf faturasında stok kodu (ItemId) bulunamaz.");

                if (!line.ExpenseDefinitionId.HasValue)
                    throw new BusinessRuleException("Masraf faturasında masraf tanımı (ExpenseDefinitionId) zorunludur.");
                
                if (expensesMap == null || !expensesMap.TryGetValue(line.ExpenseDefinitionId.Value, out var exp))
                    throw new BusinessRuleException($"Masraf tanımı {line.ExpenseDefinitionId} bulunamadı.");

                lineEntity.ExpenseDefinitionId = line.ExpenseDefinitionId;
                lineEntity.ItemCode = exp.Code;
                lineEntity.ItemName = exp.Name;
                lineEntity.Unit = "adet";
            }
            else
            {
                // Satış / Alış Faturası: ItemId zorunlu, ExpenseDefinitionId yasak
                if (line.ExpenseDefinitionId.HasValue)
                    throw new BusinessRuleException("Stok faturasında masraf tanımı (ExpenseDefinitionId) bulunamaz.");

                if (!line.ItemId.HasValue)
                     throw new BusinessRuleException("Stok faturasında ürün kodu (ItemId) zorunludur.");

                if (itemsMap == null || !itemsMap.TryGetValue(line.ItemId.Value, out var it))
                    throw new BusinessRuleException($"Item {line.ItemId} bulunamadı.");
                
                lineEntity.ItemId = line.ItemId;
                lineEntity.ItemCode = it.Code;
                lineEntity.ItemName = it.Name;
                lineEntity.Unit = it.Unit;
            }

            invoice.Lines.Add(lineEntity);

            invoice.TotalNet += lineEntity.Net;
            invoice.TotalVat += lineEntity.Vat;
            invoice.TotalGross += lineEntity.Gross;
        }

        // Her ihtimale karşı toplamları da policy ile son kez kapat (2 hane)
        invoice.TotalNet = Money.R2(invoice.TotalNet);
        invoice.TotalVat = Money.R2(invoice.TotalVat);
        invoice.TotalGross = Money.R2(invoice.TotalGross);

        // Yeni fatura oluşturulurken balance = TotalGross (henüz ödeme yok)
        invoice.Balance = invoice.TotalGross;

        // Transaction: Invoice + StockMovements birlikte commit
        await using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(ct);

            // Stok Hareketlerini Oluştur
            await CreateStockMovements(invoice, ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        // 6) Sonuç (response’ta string)
        return new CreateInvoiceResult(
            Id: invoice.Id,
            TotalNet: Money.S2(invoice.TotalNet),
            TotalVat: Money.S2(invoice.TotalVat),
            TotalGross: Money.S2(invoice.TotalGross),
            RoundingPolicy: "AwayFromZero"
        );
    }

    private async Task CreateStockMovements(Invoice invoice, CancellationToken ct)
    {
        // Fatura tipine göre Stok hareket yönünü belirle
        // Sales -> SalesOut (Çıkış)
        // SalesReturn -> SalesReturn (Giriş)
        // Purchase -> PurchaseIn (Giriş)
        // PurchaseReturn -> PurchaseReturn (Çıkış)

        StockMovementType? movementType = invoice.Type switch
        {
            InvoiceType.Sales => StockMovementType.SalesOut,
            InvoiceType.SalesReturn => StockMovementType.SalesReturn,
            InvoiceType.Purchase => StockMovementType.PurchaseIn,
            InvoiceType.PurchaseReturn => StockMovementType.PurchaseReturn,
            _ => null
        };

        if (movementType == null) return; // Proforma vb. ise hareket yok

        // Expense (Masraf) Faturası ise stok hareketi OLUŞTURMA
        if (invoice.Type == InvoiceType.Expense) return;

        // ✅ FIX: Branch'in varsayılan deposunu bul (hardcoded 1 yerine)
        var defaultWarehouse = await _db.Warehouses
            .Where(w => w.BranchId == invoice.BranchId && w.IsDefault && !w.IsDeleted)
            .Select(w => new { w.Id })
            .FirstOrDefaultAsync(ct);

        if (defaultWarehouse == null)
        {
            // Fallback: IsDefault olmasa bile şubenin ilk deposunu kullan
            defaultWarehouse = await _db.Warehouses
                .Where(w => w.BranchId == invoice.BranchId && !w.IsDeleted)
                .OrderBy(w => w.Id)
                .Select(w => new { w.Id })
                .FirstOrDefaultAsync(ct);
        }

        if (defaultWarehouse == null)
        {
            // Şubenin deposu yoksa stok hareketi oluşturulamaz
            // Loglama yapılabilir veya BusinessRuleException fırlatılabilir
            return;
        }

        foreach (var line in invoice.Lines)
        {
            // Eğer yanlışlıkla satıra Item koyulmadıysa devam et (Validasyon zaten var ama defensive coding)
            if (line.ItemId == null) continue;
            // Qty işareti: InvoiceLine.Qty'de iadelerde negatif tutuyorduk (finansal).
            // Stok servisi "mutlak değer" bekliyor olabilir, ama CreateStockMovementHandler:
            // "IsIn" ise +qty, değilse -qty yapıyor.
            // BİZİM BURADA GÖNDERECEĞİMİZ "Miktar" HER ZAMAN POZİTİF OLMALI.
            // CreateStockMovementHandler kendi içinde Type'a göre artırıp azaltacak.
            
            var absQty = Math.Abs(line.Qty); 
            if (absQty == 0) continue;

            var cmd = new Accounting.Application.StockMovements.Commands.Create.CreateStockMovementCommand(
                BranchId: invoice.BranchId,
                WarehouseId: defaultWarehouse.Id, // ✅ Dinamik warehouse
                ItemId: line.ItemId!.Value,
                Type: movementType.Value,
                Quantity: Money.S3(absQty), // String format
                TransactionDateUtc: invoice.DateUtc,
                Note: $"Fatura Ref: {invoice.Id}"
            );

            await _mediator.Send(cmd, ct);
        }
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
            "expense" => InvoiceType.Expense,
            _ => fallback
        };
    }
}
