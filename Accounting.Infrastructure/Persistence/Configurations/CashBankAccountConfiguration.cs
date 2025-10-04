using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence.Configurations; // ApplyRowVersion/ApplySoftDelete
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class CashBankAccountConfiguration : IEntityTypeConfiguration<CashBankAccount>
{
    public void Configure(EntityTypeBuilder<CashBankAccount> b)
    {
        b.ToTable("CashBankAccounts");
        b.HasKey(x => x.Id);

        b.Property(x => x.Type).HasConversion<int>().IsRequired();
        b.Property(x => x.Name).IsRequired().HasMaxLength(120);
        b.Property(x => x.Iban).HasMaxLength(34);

        // audit
        b.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd()
            .IsRequired();

        // concurrency + soft delete
        b.ApplyRowVersion();
        b.ApplySoftDelete();

        // indexes
        b.HasIndex(x => x.Type);
        b.HasIndex(x => x.Name);
    }
}
