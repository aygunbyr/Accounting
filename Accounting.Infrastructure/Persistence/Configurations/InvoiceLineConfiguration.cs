using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations
{
    public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
    {
        public void Configure(EntityTypeBuilder<InvoiceLine> builder)
        {
            builder.ToTable("InvoiceLines");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Qty).HasColumnType("decimal(18,3)");
            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
            builder.Property(x => x.VatRate).IsRequired();

            builder.Property(x => x.Net).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Vat).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Gross).HasColumnType("decimal(18,2)");

            builder.HasIndex(x => x.InvoiceId);

            // KDV 0..100 aralığı
            builder.ToTable(t => t.HasCheckConstraint("CK_InvoiceLine_VatRate_Range", "[VatRate] BETWEEN 0 AND 100"));

            // Quantity, UnitPrice >= 0
            builder.ToTable(t => t.HasCheckConstraint("CK_InvoiceLine_Qty_Positive", "[Qty] >= 0"));
            builder.ToTable(t => t.HasCheckConstraint("CK_InvoiceLine_UnitPrice_Positive", "[UnitPrice] >= 0"));

        }
    }


}
