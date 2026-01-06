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

        // Branch relationship
        b.Property(x => x.BranchId).IsRequired();
        b.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Property(x => x.Code)
            .HasMaxLength(32)
            .IsRequired();
        // Unique per branch (not globally)
        b.HasIndex(x => new { x.BranchId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        b.Property(x => x.Name)
            .HasMaxLength(128)
            .IsRequired();

        b.Property(x => x.DefaultVatRate)
            .IsRequired();

        b.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        // Timestamps
        b.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();
        b.Property(x => x.UpdatedAtUtc);

        // Soft delete
        b.Property(x => x.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();
        b.Property(x => x.DeletedAtUtc);
        b.HasQueryFilter(x => !x.IsDeleted);

        // Concurrency
        b.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}