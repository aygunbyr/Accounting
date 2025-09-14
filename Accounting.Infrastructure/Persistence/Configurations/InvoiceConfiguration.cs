using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("Invoices");

        b.HasKey(x => x.Id);

        b.Property(i => i.Type).HasConversion<int>().IsRequired();
        b.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        b.Property(x => x.DateUtc).IsRequired();

        // Totals
        b.Property(x => x.TotalNet).HasColumnType("decimal(18,2)");
        b.Property(x => x.TotalVat).HasColumnType("decimal(18,2)");
        b.Property(x => x.TotalGross).HasColumnType("decimal(18,2)");

        b.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.DateUtc);
        b.HasIndex(x => new { x.Type, x.DateUtc });

        // concurrency token
        b.Property(x => x.RowVersion).IsRowVersion();

        // soft delete
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
