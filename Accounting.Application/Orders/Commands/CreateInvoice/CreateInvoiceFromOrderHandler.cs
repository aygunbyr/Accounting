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
                ItemName = ol.Description, // Mapping description to ItemName for now
                ItemCode = ol.Item?.Code ?? "", // Need to fetch Item Code or allow nullable? InvoiceLine.ItemCode is required.
                Qty = ol.Quantity,
                UnitPrice = ol.UnitPrice,
                VatRate = ol.VatRate,
                Net = ol.Total, // OrderLine.Total is Net
                Vat = Money.R2(ol.Total * ol.VatRate / 100m),
                Gross = Money.R2(ol.Total + (ol.Total * ol.VatRate / 100m))
            });
        }

        // Update Order Status
        order.Status = OrderStatus.Invoiced;
        order.UpdatedAtUtc = DateTime.UtcNow;

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        return invoice.Id;
    }
}
