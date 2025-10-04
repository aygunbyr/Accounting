using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public enum ContactType { Customer = 1, Vendor = 2 }

public class Contact : IHasTimestamps, ISoftDeletable, IHasRowVersion
{
    public int Id { get; set; }
    public ContactType Type { get; set; } = ContactType.Customer;
    public string Name { get; set; } = null!;
    public string? TaxNo { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // unified timestamps
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    // soft delete + concurrency
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
