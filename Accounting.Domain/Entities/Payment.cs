namespace Accounting.Domain.Entities;

public enum PaymentDirection { In = 1, Out = 2 }
public class Payment
{
    public int Id { get; set; }
    public int AccountId { get; set; } // Cash/Bank
    public int? ContactId { get; set; } // optional
    public int? LinkedInvoiceId { get; set; } // invoice
    public DateTime DateUtc { get; set; } = DateTime.UtcNow;
    public PaymentDirection Direction { get; set; } = PaymentDirection.In;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";

    public bool IsDeleted { get; set; }
    public byte[] RowVersion { get; set; } = null!;

}
