using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public class InvoiceLine : IHasTimestamps, ISoftDeletable
{
    public int Id { get; set; }

    // FK'ler
    public int InvoiceId { get; set; }
    public int? ItemId { get; set; }
    public int? ExpenseDefinitionId { get; set; }

    // ✅ Snapshot alanlar (o anın kopyası)
    public string ItemCode { get; set; } = null!;
    public string ItemName { get; set; } = null!;
    public string Unit { get; set; } = "adet";   // örn: adet, kg, lt

    // Snapshot alanlar (fiyat/KDV o anki kurallarla sabitlenir)
    public decimal Qty { get; set; }        // 18,3
    public decimal UnitPrice { get; set; }  // 18,4
    public int VatRate { get; set; }        // 0..100

    // Türemiş/saklanan tutarlar (AwayFromZero, 2 hane)
    public decimal Net { get; set; }        // 18,2
    public decimal Vat { get; set; }        // 18,2
    public decimal Gross { get; set; }      // 18,2

    // Timestamps
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    // Navigations
    public Invoice Invoice { get; set; } = null!;
    public Item? Item { get; set; }
    public ExpenseDefinition? ExpenseDefinition { get; set; }
}
