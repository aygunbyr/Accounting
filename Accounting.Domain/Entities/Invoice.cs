using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public enum InvoiceType { Sales = 1, Purchase = 2 }

public class Invoice : IHasTimestamps, ISoftDeletable, IHasRowVersion
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    public InvoiceType Type { get; set; } = InvoiceType.Sales;
    public DateTime DateUtc { get; set; } = DateTime.UtcNow;
    public string Currency { get; set; } = "TRY";

    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }

    public List<InvoiceLine> Lines { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    public Contact Contact { get; set; } = null!;
}
