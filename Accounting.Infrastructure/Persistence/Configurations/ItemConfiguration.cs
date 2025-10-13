using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence.Configurations; // ApplyRowVersion/ApplySoftDelete
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> b)
    {
        b.ToTable("Items");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).IsRequired().HasMaxLength(160);
        b.Property(x => x.Unit).IsRequired().HasMaxLength(16);
        b.Property(x => x.VatRate).IsRequired();

        b.Property(x => x.DefaultUnitPrice).HasColumnType("decimal(18,4)");

        // audit
        b.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd()
            .IsRequired();

        // concurrency + soft delete
        b.ApplyRowVersion();
        b.ApplySoftDelete();

        // indexes / constraints
        b.HasIndex(x => x.Name).HasDatabaseName("IX_Items_Name");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Item_VatRate_Range", "[VatRate] BETWEEN 0 AND 100");
        });
    }
}
