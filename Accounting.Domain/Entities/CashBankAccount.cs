using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public enum CashBankAccountType { Cash = 1, Bank = 2 }

public class CashBankAccount : IHasTimestamps, ISoftDeletable, IHasRowVersion
{
    public int Id { get; set; }
    public CashBankAccountType Type { get; set; } = CashBankAccountType.Cash;
    public string Name { get; set; } = null!;
    public string? Iban { get; set; }

    // audit + soft delete + concurrency
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
