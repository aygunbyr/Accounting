using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public sealed class ExpenseDefinitionConfiguration : IEntityTypeConfiguration<ExpenseDefinition>
{
    public void Configure(EntityTypeBuilder<ExpenseDefinition> b)
    {
        b.ToTable("ExpenseDefinitions", t =>
        {
            t.HasCheckConstraint("CK_ExpenseDef_VatRange", "[DefaultVatRate] BETWEEN 0 AND 100");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();

        b.Property(x => x.Code)
            .HasMaxLength(32)
            .IsRequired();
        b.HasIndex(x => x.Code).IsUnique();

        b.Property(x => x.Name)
            .HasMaxLength(128)
            .IsRequired();

        b.Property(x => x.DefaultVatRate)
            .IsRequired();

        b.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        b.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        b.Property(x => x.UpdatedAtUtc);

        b.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}