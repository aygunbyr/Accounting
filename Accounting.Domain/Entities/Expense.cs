namespace Accounting.Domain.Entities;

public class Expense
{
    public int Id { get; set; }

    public int ExpenseListId { get; set; }
    public ExpenseList ExpenseList { get; set; } = null!;

    public DateTime DateUtc { get; set; } = DateTime.UtcNow;

    public int? SupplierId { get; set; } // Contact Id optional
    public string Currency { get; set; } = "TRY";

    public decimal Amount { get; set; }
    public int VatRate { get; set; }

    public string? Category { get; set; }
    public string? Notes { get; set; }

    // Post edildiğinde bağlandığı invoice id (satır seviyesinde de iz bırakmak isteyebiliriz)
    public int? PostedInvoiceId { get; set; }
}
