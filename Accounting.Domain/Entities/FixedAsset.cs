using Accounting.Domain.Common;

namespace Accounting.Domain.Entities;

public class FixedAsset : IHasTimestamps, ISoftDeletable, IHasRowVersion, IHasBranch
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string Code { get; set; } = null!;        // benzersiz, max 32
    public string Name { get; set; } = null!;        // max 128

    public DateTime PurchaseDateUtc { get; set; }    // alış tarihi (UTC)
    public decimal PurchasePrice { get; set; }       // alış tutarı (decimal)

    /// <summary>
    /// Faydalı ömür (yıl). 1..50 gibi bir aralıkta beklenir.
    /// </summary>
    public int UsefulLifeYears { get; set; }

    /// <summary>
    /// Amortisman oranı (%) — 100 / UsefulLifeYears olarak hesaplanır, snapshot olarak tutulur.
    /// </summary>
    public decimal DepreciationRatePercent { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Navigations
    public Branch Branch { get; set; } = null!;
}