namespace Accounting.Domain.Entities;


public enum InvoiceType { Sales = 1, Purchase = 2 }

public class Invoice
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public InvoiceType Type { get; set; } = InvoiceType.Sales;
    public DateTime DateUtc { get; set; } = DateTime.UtcNow; // UTC persist
    public string Currency { get; set; } = "TRY";

    // Totals -> decimal(18,2)
    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }

    public List<InvoiceLine> Lines { get; set; } = new();

    public bool IsDeleted { get; set; } // soft delete
    public byte[] RowVersion { get; set; } = null!; // optimistic concurrency

}
