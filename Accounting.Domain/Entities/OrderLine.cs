using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public class OrderLine : IHasTimestamps
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int? ItemId { get; set; }

    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int VatRate { get; set; } // 0, 1, 10, 20
    public decimal Total { get; set; } // Net + Vat ? No, usually Line Total is Net or Gross. Let's assume Net like InvoiceLine or strictly (Qty * Price)

    // Audit
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    // Navigations
    public Order Order { get; set; } = null!;
    public Item? Item { get; set; }
}
