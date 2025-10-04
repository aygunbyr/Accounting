using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public enum PaymentDirection { In = 1, Out = 2 }

public class Payment : IHasTimestamps, ISoftDeletable, IHasRowVersion
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int? ContactId { get; set; }
    public int? LinkedInvoiceId { get; set; }
    public DateTime DateUtc { get; set; } = DateTime.UtcNow;
    public PaymentDirection Direction { get; set; } = PaymentDirection.In;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
