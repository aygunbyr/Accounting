using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");

        b.HasKey(x => x.Id);

        b.Property(x => x.OrderNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.OrderNumber).IsUnique();

        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.Property(x => x.Description).HasMaxLength(200);

        b.Property(x => x.TotalNet).HasPrecision(18, 2);
        b.Property(x => x.TotalVat).HasPrecision(18, 2);
        b.Property(x => x.TotalGross).HasPrecision(18, 2);

        // Audit + Soft Delete + RowVersion
        b.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
        b.ApplySoftDelete();
        b.ApplyRowVersion();

        // Relationships
        b.HasOne(x => x.Contact)
            .WithMany()
            .HasForeignKey(x => x.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        // Lines
        b.HasMany(x => x.Lines)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
