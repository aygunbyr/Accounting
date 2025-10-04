using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");

        b.HasKey(x => x.Id);

        b.Property(x => x.Direction).HasConversion<int>();
        b.Property(x => x.DateUtc).IsRequired();
        b.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");

        b.HasIndex(x => x.DateUtc);
        b.HasIndex(x => new { x.Direction, x.DateUtc });

        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
