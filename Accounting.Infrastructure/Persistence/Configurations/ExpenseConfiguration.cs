using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> b)
    {
        b.ToTable("Expenses");
        b.HasKey(x => x.Id);

        b.Property(x => x.DateUtc).IsRequired();

        b.Property(x => x.SupplierId).IsRequired(false);

        b.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        b.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)") // money2
            .IsRequired();

        b.Property(x => x.VatRate).IsRequired(); // 0..100

        b.Property(x => x.Category).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);

        b.Property(x => x.PostedInvoiceId).IsRequired(false);

        b.HasIndex(x => x.DateUtc);
        b.HasIndex(x => new { x.SupplierId, x.DateUtc });
    }
}
