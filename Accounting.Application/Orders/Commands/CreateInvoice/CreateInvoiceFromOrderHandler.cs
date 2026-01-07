using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Application.Common.Utils;
using Accounting.Application.Invoices.Queries.Dto;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Orders.Commands.CreateInvoice;

public record CreateInvoiceFromOrderCommand(int OrderId) : IRequest<int>;

public class CreateInvoiceFromOrderHandler(IAppDbContext db) : IRequestHandler<CreateInvoiceFromOrderCommand, int>
{
    public async Task<int> Handle(CreateInvoiceFromOrderCommand r, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Lines)
                .ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(o => o.Id == r.OrderId && !o.IsDeleted, ct);

        if (order is null) throw new NotFoundException("Order", r.OrderId);

        if (order.Status != OrderStatus.Approved)
        {
            throw new BusinessRuleException("Sadece onaylı siparişler faturaya dönüştürülebilir.");
        }

        // Create Invoice
        var invoice = new Invoice
        {
            BranchId = order.BranchId,
            ContactId = order.ContactId,
            OrderId = order.Id,
            Type = order.Type,
            DateUtc = DateTime.UtcNow,
            Currency = order.Currency,
            TotalNet = order.TotalNet,
            TotalVat = order.TotalVat,
            TotalGross = order.TotalGross,
            Balance = order.TotalGross, // Initially unpaid
            InvoiceNumber = $"INV-{order.OrderNumber}", // Simple generation
            CreatedAtUtc = DateTime.UtcNow,
            RowVersion = []
        };

        foreach (var ol in order.Lines)
        {
            invoice.Lines.Add(new InvoiceLine
            {
                ItemId = ol.ItemId,
                ItemName = ol.Item?.Name ?? ol.Description,
                ItemCode = ol.Item?.Code ?? "-", // Fallback for non-item lines
                Qty = ol.Quantity,
                UnitPrice = ol.UnitPrice,
                VatRate = ol.VatRate,
                Net = ol.Total,
                Vat = Money.R2(ol.Total * ol.VatRate / 100m),
                Gross = Money.R2(ol.Total + (ol.Total * ol.VatRate / 100m))
            });
        }

        // Update Order Status
        order.Status = OrderStatus.Invoiced;
        order.UpdatedAtUtc = DateTime.UtcNow;

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        // Create StockMovements for item-based lines (Sales = Out, Purchase = In)
        await CreateStockMovementsAsync(invoice, order.BranchId, ct);

        return invoice.Id;
    }

    private async Task CreateStockMovementsAsync(Invoice invoice, int branchId, CancellationToken ct)
    {
        // Only process lines with ItemId (physical products)
        var itemLines = invoice.Lines.Where(l => l.ItemId.HasValue).ToList();
        if (!itemLines.Any()) return;

        // Get default warehouse for this branch
        var defaultWarehouse = await db.Warehouses
            .Where(w => w.BranchId == branchId && w.IsDefault && !w.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (defaultWarehouse == null)
        {
            // Fallback: use first warehouse of branch
            defaultWarehouse = await db.Warehouses
                .Where(w => w.BranchId == branchId && !w.IsDeleted)
                .FirstOrDefaultAsync(ct);
        }

        if (defaultWarehouse == null) return; // No warehouse, skip stock movements

        // Determine movement type based on invoice type
        bool isOutgoing = invoice.Type == InvoiceType.Sales || invoice.Type == InvoiceType.PurchaseReturn;
        var movementType = isOutgoing ? StockMovementType.SalesOut : StockMovementType.PurchaseIn;

        foreach (var line in itemLines)
        {
            var itemId = line.ItemId!.Value;

            // Create movement
            var movement = new StockMovement
            {
                BranchId = branchId,
                WarehouseId = defaultWarehouse.Id,
                ItemId = itemId,
                Type = movementType,
                Quantity = line.Qty,
                TransactionDateUtc = invoice.DateUtc,
                Note = $"Fatura Ref: {invoice.Id} (Sipariş: {invoice.OrderId})",
                RowVersion = []
            };
            db.StockMovements.Add(movement);

            // Update stock
            var stock = await db.Stocks.FirstOrDefaultAsync(s =>
                s.BranchId == branchId &&
                s.WarehouseId == defaultWarehouse.Id &&
                s.ItemId == itemId &&
                !s.IsDeleted, ct);

            if (stock == null)
            {
                stock = new Stock
                {
                    BranchId = branchId,
                    WarehouseId = defaultWarehouse.Id,
                    ItemId = itemId,
                    Quantity = 0,
                    RowVersion = []
                };
                db.Stocks.Add(stock);
            }

            // Update quantity
            if (isOutgoing)
                stock.Quantity = Money.R3(stock.Quantity - line.Qty);
            else
                stock.Quantity = Money.R3(stock.Quantity + line.Qty);
        }

        await db.SaveChangesAsync(ct);
    }
}
