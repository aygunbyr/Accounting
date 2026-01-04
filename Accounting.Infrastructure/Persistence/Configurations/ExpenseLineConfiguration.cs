using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class ExpenseLineConfiguration : IEntityTypeConfiguration<ExpenseLine>
{
    public void Configure(EntityTypeBuilder<ExpenseLine> b)
    {
        b.ToTable("ExpenseLines");
        b.HasKey(x => x.Id);

        // relation
        b.HasOne(x => x.ExpenseList)
            .WithMany(l => l.Lines)
            .HasForeignKey(x => x.ExpenseListId)
            .OnDelete(DeleteBehavior.Cascade);

        // fields
        b.Property(x => x.DateUtc).IsRequired();
        b.Property(x => x.SupplierId).IsRequired(false);

        b.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        b.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        b.Property(x => x.VatRate).IsRequired(); // 0..100

        b.Property(x => x.Category).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);

        b.Property(x => x.PostedInvoiceId).IsRequired(false);

        // audit
        b.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd()
            .IsRequired();

        // concurrency + soft delete
        b.ApplyRowVersion();
        b.ApplySoftDelete();

        // indexes
        b.HasIndex(x => x.DateUtc);
        b.HasIndex(x => new { x.SupplierId, x.DateUtc });

        // opsiyonel check constraints:
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ExpenseLine_VatRate_Range", "[VatRate] BETWEEN 0 AND 100");
        });
    }
}
