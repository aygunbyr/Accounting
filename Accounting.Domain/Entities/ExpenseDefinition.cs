namespace Accounting.Domain.Entities;

public class ExpenseDefinition
{
    public int Id { get; set; }                  // Identity
    public string Code { get; set; } = null!;    // Unique, max 32
    public string Name { get; set; } = null!;    // max 128
    public int DefaultVatRate { get; set; }      // 0..100 (yüzde)
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>(); // rowversion/timestamp
}
