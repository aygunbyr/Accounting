using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public sealed class FixedAssetConfiguration : IEntityTypeConfiguration<FixedAsset>
{
    public void Configure(EntityTypeBuilder<FixedAsset> b)
    {
        b.ToTable("FixedAssets", t =>
        {
            // Faydalı ömür: 1..100; Amortisman oranı: 0..100
            t.HasCheckConstraint("CK_FixedAssets_UsefulLife",
                "[UsefulLifeYears] >= 1 AND [UsefulLifeYears] <= 100");
            t.HasCheckConstraint("CK_FixedAssets_DepRate",
                "[DepreciationRatePercent] >= 0 AND [DepreciationRatePercent] <= 100");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.Property(x => x.Code)
            .HasMaxLength(32)
            .IsRequired();
        b.HasIndex(x => x.Code)
            .IsUnique();

        b.Property(x => x.Name)
            .HasMaxLength(128)
            .IsRequired();

        b.Property(x => x.PurchaseDateUtc)
            .IsRequired();

        b.Property(x => x.PurchasePrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        b.Property(x => x.UsefulLifeYears)
            .IsRequired();

        b.Property(x => x.DepreciationRatePercent)
            .HasColumnType("decimal(9,4)") // 2.50, 20.0000 vs.
            .IsRequired();

        b.Property(x => x.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        b.Property(x => x.DeletedAtUtc);

        b.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        b.Property(x => x.UpdatedAtUtc);

        b.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}
