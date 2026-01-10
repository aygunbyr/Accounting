using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Utils;
using Accounting.Application.Invoices.Queries.Dto;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Accounting.Application.Common.Interfaces;

public sealed class UpdateInvoiceHandler : IRequestHandler<UpdateInvoiceCommand, InvoiceDto>
{
    private readonly IAppDbContext _ctx;
    private readonly IInvoiceBalanceService _balanceService;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public UpdateInvoiceHandler(IAppDbContext ctx, IInvoiceBalanceService balanceService, IMediator mediator, ICurrentUserService currentUserService)
    {
        _ctx = ctx;
        _balanceService = balanceService;
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    public async Task<InvoiceDto> Handle(UpdateInvoiceCommand r, CancellationToken ct)
    {
        // 1) Aggregate (+Include)
        var inv = await _ctx.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == r.Id, ct)
            ?? throw new NotFoundException(nameof(Invoice), r.Id);

        // Security Check: Must be in same branch
        var branchId = _currentUserService.BranchId ?? throw new UnauthorizedAccessException();
        if (inv.BranchId != branchId)
            throw new NotFoundException(nameof(Invoice), r.Id); // Hide cross-branch existence or Unauthorized

        // 2) Concurrency (RowVersion base64)
        _ctx.Entry(inv).Property(nameof(Invoice.RowVersion))
            .OriginalValue = Convert.FromBase64String(r.RowVersionBase64);

        // 3) Normalize (parent)
        inv.Currency = (r.Currency ?? "TRY").Trim().ToUpperInvariant();
        inv.DateUtc = r.DateUtc;
        inv.ContactId = r.ContactId;
        // inv.BranchId assignment removed - Branch cannot be changed
        inv.Type = NormalizeType(r.Type, inv.Type);

        // ---- Satır diff senkronu ----
        var now = DateTime.UtcNow;

        // sign: removed (All positive)

        // Snapshot için gerekli Item'ları ve Expense'leri tek seferde çek
        var allItemIds = r.Lines.Where(x => x.ItemId.HasValue).Select(x => x.ItemId!.Value).Distinct().ToList();
        var allExpenseIds = r.Lines.Where(x => x.ExpenseDefinitionId.HasValue).Select(x => x.ExpenseDefinitionId!.Value).Distinct().ToList();

        var itemsMap = await _ctx.Items
            .Where(i => allItemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Code, i.Name, i.Unit, i.VatRate })
            .ToDictionaryAsync(i => i.Id, ct);

        var expensesMap = await _ctx.ExpenseDefinitions
            .Where(i => allExpenseIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Code, i.Name })
            .ToDictionaryAsync(i => i.Id, ct);

        var incomingById = r.Lines.Where(x => x.Id > 0).ToDictionary(x => x.Id);

        // a) Silinecekler: mevcutta var, body’de yok
        foreach (var line in inv.Lines.ToList())
        {
            if (!incomingById.ContainsKey(line.Id))
            {
                // Soft delete (audit trail korunur)
                line.IsDeleted = true;
                line.DeletedAtUtc = now;
            }
        }

        // b) Güncellenecekler
        foreach (var line in inv.Lines)
        {
            if (incomingById.TryGetValue(line.Id, out var dto))
            {
                // ✅ String → decimal parse
                if (!Money.TryParse4(dto.Qty, out var qty))
                    throw new BusinessRuleException($"Line {line.Id}: Invalid Qty format.");
                if (!Money.TryParse4(dto.UnitPrice, out var unitPrice))
                    throw new BusinessRuleException($"Line {line.Id}: Invalid UnitPrice format.");

                // Qty logic: DB Positive
                var absQty = Math.Abs(qty);

                line.Qty = Money.R3(absQty);           // ✅ Positive for DB
                line.UnitPrice = Money.R4(unitPrice);
                line.VatRate = dto.VatRate;
                line.UpdatedAtUtc = now;

                // Type'a göre logic
                if (inv.Type == InvoiceType.Expense)
                {
                    if (dto.ItemId.HasValue) throw new BusinessRuleException("Masraf faturasında ItemId olamaz.");
                    if (!dto.ExpenseDefinitionId.HasValue) throw new BusinessRuleException("Masraf faturasında ExpenseDefinitionId zorunludur.");

                    var expChanged = line.ExpenseDefinitionId != dto.ExpenseDefinitionId;
                    line.ExpenseDefinitionId = dto.ExpenseDefinitionId;
                    line.ItemId = null; // Ensure null

                    // Snapshot
                    if (expensesMap.TryGetValue(line.ExpenseDefinitionId.Value, out var exp) && (expChanged || string.IsNullOrWhiteSpace(line.ItemCode)))
                    {
                        line.ItemCode = exp.Code;
                        line.ItemName = exp.Name;
                        line.Unit = "adet";
                    }
                }
                else
                {
                    if (dto.ExpenseDefinitionId.HasValue) throw new BusinessRuleException("Stok faturasında ExpenseDefinitionId olamaz.");
                    if (!dto.ItemId.HasValue) throw new BusinessRuleException("Stok faturasında ItemId zorunludur.");

                    var itemChanged = line.ItemId != dto.ItemId;
                    line.ItemId = dto.ItemId;
                    line.ExpenseDefinitionId = null;

                    // Snapshot
                    if (itemsMap.TryGetValue(line.ItemId.Value, out var it) && (itemChanged || string.IsNullOrWhiteSpace(line.ItemCode)))
                    {
                        line.ItemCode = it.Code;
                        line.ItemName = it.Name;
                        line.Unit = it.Unit;
                    }
                }

                // Hesaplar (AwayFromZero)
                var net = Money.R2(unitPrice * absQty);
                var vat = Money.R2(net * line.VatRate / 100m);
                var gross = Money.R2(net + vat);

                line.Net = Money.R2(net); // Positive
                line.Vat = Money.R2(vat); // Positive
                line.Gross = Money.R2(gross); // Positive
            }
        }

        // c) Yeni satırlar
        foreach (var dto in r.Lines.Where(x => x.Id == 0))
        {
            // ✅ String → decimal parse
            if (!Money.TryParse4(dto.Qty, out var qty))
                throw new BusinessRuleException($"New line: Invalid Qty format.");
            if (!Money.TryParse4(dto.UnitPrice, out var unitPrice))
                throw new BusinessRuleException($"New line: Invalid UnitPrice format.");

            // Qty Logic
            var absQty = Math.Abs(qty);

            var net = Money.R2(unitPrice * absQty);
            var vat = Money.R2(net * dto.VatRate / 100m);
            var gross = Money.R2(net + vat);

            var nl = new InvoiceLine
            {
                ItemId = dto.ItemId,
                ExpenseDefinitionId = dto.ExpenseDefinitionId,
                Qty = Money.R3(absQty),             // ✅ Positive for DB
                UnitPrice = Money.R4(unitPrice),
                VatRate = dto.VatRate,
                Net = Money.R2(net),
                Vat = Money.R2(vat),
                Gross = Money.R2(gross),
                CreatedAtUtc = now
            };

            if (inv.Type == InvoiceType.Expense)
            {
                if (dto.ItemId.HasValue) throw new BusinessRuleException("Masraf faturasında ItemId olamaz.");
                if (!dto.ExpenseDefinitionId.HasValue) throw new BusinessRuleException("Masraf faturasında ExpenseDefinitionId zorunludur.");

                if (expensesMap.TryGetValue(dto.ExpenseDefinitionId.Value, out var exp))
                {
                    nl.ItemCode = exp.Code;
                    nl.ItemName = exp.Name;
                    nl.Unit = "adet";
                }
                else throw new BusinessRuleException("Masraf tanımı bulunamadı.");
            }
            else
            {
                if (dto.ExpenseDefinitionId.HasValue) throw new BusinessRuleException("Stok faturasında ExpenseDefinitionId olamaz.");
                if (!dto.ItemId.HasValue) throw new BusinessRuleException("Stok faturasında ItemId zorunludur.");

                if (itemsMap.TryGetValue(dto.ItemId.Value, out var it))
                {
                    nl.ItemCode = it.Code;
                    nl.ItemName = it.Name;
                    nl.Unit = it.Unit;
                }
                else throw new BusinessRuleException("Stok kartı bulunamadı.");
            }

            inv.Lines.Add(nl);
        }

        // 4) UpdatedAt + parent toplamlar (satırlar zaten işaretli)
        inv.UpdatedAtUtc = now;
        inv.TotalNet = Money.R2(inv.Lines.Sum(x => x.Net));
        inv.TotalVat = Money.R2(inv.Lines.Sum(x => x.Vat));
        inv.TotalGross = Money.R2(inv.Lines.Sum(x => x.Gross));

        // Toplamlar değişince bakiyeyi yeniden hesapla
        await _balanceService.RecalculateBalanceAsync(inv.Id, ct);

        // Transaction: Invoice update + StockMovements birlikte commit
        await using var tx = await _ctx.BeginTransactionAsync(ct);
        try
        {
            // 5) Save + concurrency
            try { await _ctx.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException)
            { throw new ConcurrencyConflictException(); }

            // 5.5) Stok Hareketlerini Senkronize Et (Reset yöntemi)
            await SyncStockMovements(inv, ct);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

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
                l.ExpenseDefinitionId, // Added new field
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

    private async Task SyncStockMovements(Invoice invoice, CancellationToken ct)
    {
        // 1. Stok hareketi gerekmeyen durum (Expense)
        if (invoice.Type == InvoiceType.Expense) return;

        // 2. Mevcut hareketleri bul ve sil (Reset) - InvoiceId ile
        var existingMovements = await _ctx.StockMovements
            .Where(m => m.InvoiceId == invoice.Id && !m.IsDeleted)
            .ToListAsync(ct);

        foreach (var move in existingMovements)
        {
            move.IsDeleted = true;
            move.DeletedAtUtc = DateTime.UtcNow;
        }
        await _ctx.SaveChangesAsync(ct);

        // 3. Yeni hareketleri oluştur
        StockMovementType? movementType = invoice.Type switch
        {
            InvoiceType.Sales => StockMovementType.SalesOut,
            InvoiceType.SalesReturn => StockMovementType.SalesReturn,
            InvoiceType.Purchase => StockMovementType.PurchaseIn,
            InvoiceType.PurchaseReturn => StockMovementType.PurchaseReturn,
            _ => null
        };

        if (movementType == null) return;

        // ✅ FIX: Branch'in varsayılan deposunu bul (hardcoded 1 yerine)
        var defaultWarehouse = await _ctx.Warehouses
            .Where(w => w.BranchId == invoice.BranchId && w.IsDefault && !w.IsDeleted)
            .Select(w => new { w.Id })
            .FirstOrDefaultAsync(ct);

        if (defaultWarehouse == null)
        {
            // Fallback: IsDefault olmasa bile şubenin ilk deposunu kullan
            defaultWarehouse = await _ctx.Warehouses
                .Where(w => w.BranchId == invoice.BranchId && !w.IsDeleted)
                .OrderBy(w => w.Id)
                .Select(w => new { w.Id })
                .FirstOrDefaultAsync(ct);
        }

        if (defaultWarehouse == null)
        {
            // Şubenin deposu yoksa stok hareketi oluşturulamaz
            return;
        }

        foreach (var line in invoice.Lines)
        {
            if (line.ItemId == null) continue;

            var absQty = line.Qty; // DB'de zaten pozitif (+)
            if (absQty == 0) continue;

            // Create command
            var cmd = new Accounting.Application.StockMovements.Commands.Create.CreateStockMovementCommand(
                WarehouseId: defaultWarehouse.Id, // ✅ Dinamik warehouse
                ItemId: line.ItemId.Value,
                Type: movementType.Value,
                Quantity: Money.S3(absQty),
                TransactionDateUtc: invoice.DateUtc,
                Note: null,
                InvoiceId: invoice.Id // FK ile ilişkilendirme
            );

            await _mediator.Send(cmd, ct);
        }
    }
}