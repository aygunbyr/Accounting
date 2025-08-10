using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Direction).HasConversion<int>();
        builder.Property(x => x.DateUtc).IsRequired();
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");

        builder.HasIndex(x => x.DateUtc);
        builder.HasIndex(x => new { x.Direction, x.DateUtc });
    }
}
