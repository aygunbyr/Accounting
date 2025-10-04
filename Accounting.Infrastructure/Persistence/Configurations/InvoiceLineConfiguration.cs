using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> b)
    {
        b.ToTable("InvoiceLines");
        b.HasKey(x => x.Id);

        b.Property(x => x.Qty).HasColumnType("decimal(18,3)");
        b.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
        b.Property(x => x.VatRate).IsRequired();

        b.Property(x => x.Net).HasColumnType("decimal(18,2)");
        b.Property(x => x.Vat).HasColumnType("decimal(18,2)");
        b.Property(x => x.Gross).HasColumnType("decimal(18,2)");

        // timestamps
        b.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd()
            .IsRequired();

        b.HasIndex(x => x.InvoiceId);

        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_InvoiceLine_VatRate_Range", "[VatRate] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_InvoiceLine_Qty_Positive", "[Qty] >= 0");
            t.HasCheckConstraint("CK_InvoiceLine_UnitPrice_Positive", "[UnitPrice] >= 0");
        });
    }
}
