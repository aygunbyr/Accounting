using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("Invoices");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Direction).HasConversion<int>();
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.DateUtc).IsRequired();

            // Totals
            builder.Property(x => x.TotalNet).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalVat).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalGross).HasColumnType("decimal(18,2)");

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.DateUtc);
            builder.HasIndex(x => new { x.Direction, x.DateUtc });
        }
    }


}
