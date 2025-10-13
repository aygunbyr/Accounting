using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence.Configurations; // ApplyRowVersion/ApplySoftDelete
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

        // audit
        b.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd()
            .IsRequired();

        // concurrency + soft delete
        b.ApplyRowVersion();
        b.ApplySoftDelete();

        // indexes
        b.HasIndex(x => x.DateUtc).HasDatabaseName("IX_Payments_DateUtc");
        b.HasIndex(x => x.AccountId).HasDatabaseName("IX_Payments_AccountId");
        b.HasIndex(x => x.ContactId).HasDatabaseName("IX_Payments_ContactId");
        b.HasIndex(x => x.Currency).HasDatabaseName("IX_Payments_Currency");
    }
}
