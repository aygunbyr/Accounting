using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class CashBankAccountConfiguration : IEntityTypeConfiguration<CashBankAccount>
{
    public void Configure(EntityTypeBuilder<CashBankAccount> builder)
    {
        builder.ToTable("CashBankAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Iban).HasMaxLength(34);

        builder.HasIndex(x => x.Type);
    }
}
