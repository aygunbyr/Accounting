using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public class Item : IHasTimestamps, ISoftDeletable, IHasRowVersion, IHasBranch
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Unit { get; set; } = "adet";
    public int VatRate { get; set; } = 20;
    public decimal? DefaultUnitPrice { get; set; }

    // audit + soft delete + concurrency
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    // Navigations
    public Branch Branch { get; set; } = null!;
}
