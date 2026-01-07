using Accounting.Domain.Common;
using Accounting.Domain.Enums;

namespace Accounting.Domain.Entities;

public class Invoice : IHasTimestamps, ISoftDeletable, IHasRowVersion, IHasBranch
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int ContactId { get; set; }
    public int? OrderId { get; set; } // Link to Order
    public InvoiceType Type { get; set; } = InvoiceType.Sales;
    public DateTime DateUtc { get; set; } = DateTime.UtcNow;
    public string InvoiceNumber { get; set; } = null!;
    public string Currency { get; set; } = "TRY";

    public decimal TotalNet { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalGross { get; set; }
    public decimal Balance { get; set; }

    public List<InvoiceLine> Lines { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    public Contact Contact { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
    public Order? Order { get; set; } // Sipariş kaynaklı faturalar için
}
