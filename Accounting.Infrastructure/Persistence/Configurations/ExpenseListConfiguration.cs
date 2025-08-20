using Accounting.Domain.Entities;
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
        b.Property(x => x.CreatedUtc).IsRequired();

        b.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        b.Property(x => x.PostedInvoiceId).IsRequired(false);

        b.HasMany(x => x.Lines)
            .WithOne(x => x.ExpenseList)
            .HasForeignKey(x => x.ExpenseListId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CreatedUtc);
    }
}
