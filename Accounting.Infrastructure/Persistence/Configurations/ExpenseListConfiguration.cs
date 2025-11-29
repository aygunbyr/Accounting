using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence.Configurations; // ApplyRowVersion/ApplySoftDelete
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class ExpenseListConfiguration : IEntityTypeConfiguration<ExpenseList>
{
    public void Configure(EntityTypeBuilder<ExpenseList> b)
    {
        b.ToTable("ExpenseLists");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(200).IsRequired();

        b.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        b.Property(x => x.PostedInvoiceId).IsRequired(false);

        b.HasMany(x => x.Lines)
            .WithOne(x => x.ExpenseList)
            .HasForeignKey(x => x.ExpenseListId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedAtUtc);

        // audit
        b.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd()
            .IsRequired();

        b.HasIndex(i => i.BranchId).HasDatabaseName("IX_ExpenseLists_BranchId");

        // concurrency + soft delete
        b.ApplyRowVersion();
        b.ApplySoftDelete();

        b.HasOne(i => i.Branch)
            .WithMany()
            .HasForeignKey(i => i.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
