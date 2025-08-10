namespace Accounting.Domain.Entities;


public enum InvoiceDirection { Sale = 1, Purchase = 2 }

public class Invoice
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public InvoiceDirection Direction { get; set; }
    public DateTime DateUtc { get; set; } = DateTime.UtcNow; // UTC persist
    public string Currency { get; set; } = "TRY";

    // Totals -> decimal(18,2)
    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }

    public List<InvoiceLine> Lines { get; set; } = new();

}
