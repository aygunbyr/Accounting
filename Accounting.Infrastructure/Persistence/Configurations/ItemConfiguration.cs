using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Unit).IsRequired().HasMaxLength(16);
        builder.Property(x => x.VatRate).IsRequired();

        builder.Property(x => x.DefaultUnitPrice).HasColumnType("decimal(18,4)");

        // KDV oranı 0..100 aralığı (DB tarafı güvence)
        builder.ToTable(t => t.HasCheckConstraint("CK_Item_VatRate_Range", "[VatRate] BETWEEN 0 AND 100"));
    }
}
