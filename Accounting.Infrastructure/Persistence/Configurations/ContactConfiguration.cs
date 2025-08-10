using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations
{
    public class ContactConfiguration : IEntityTypeConfiguration<Contact>
    {
        public void Configure(EntityTypeBuilder<Contact> builder)
        {
            builder.ToTable("Contacts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type).HasConversion<int>();
            builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
            builder.Property(x => x.TaxNo).HasMaxLength(20);
            builder.Property(x => x.Email).HasMaxLength(160);
            builder.Property(x => x.Phone).HasMaxLength(40);

            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.Type);
        }
    }

    public class ItemConfiguration : IEntityTypeConfiguration<Item>
    {
        public void Configure(EntityTypeBuilder<Item> builder)
        {
            builder.ToTable("Items");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(160);
            builder.Property(x => x.Unit).IsRequired().HasMaxLength(16);
            builder.Property(x => x.VatRate).IsRequired();

            builder.Property(x => x.DefaultUnitPrice).HasColumnType("decimal(18,4)");

            // KDV oranı 0..100 aralığı (DB tarafı güvence)
            builder.ToTable(t => t.HasCheckConstraint("CK_Item_VatRate_Range", "[VatRate] BETWEEN 0 AND 100"));
        }
    }

    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("Invoices");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Direction).HasConversion<int>();
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            builder.Property(x => x.DateUtc).IsRequired();

            // Totals
            builder.Property(x => x.TotalNet).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalVat).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalGross).HasColumnType("decimal(18,2)");

            builder.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.DateUtc);
            builder.HasIndex(x => new { x.Direction, x.DateUtc });
        }
    }

    public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
    {
        public void Configure(EntityTypeBuilder<InvoiceLine> builder)
        {
            builder.ToTable("InvoiceLines");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Qty).HasColumnType("decimal(18,3)");
            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,4)");
            builder.Property(x => x.VatRate).IsRequired();

            builder.Property(x => x.Net).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Vat).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Gross).HasColumnType("decimal(18,2)");

            builder.HasIndex(x => x.InvoiceId);

            // KDV 0..100 aralığı
            builder.ToTable(t => t.HasCheckConstraint("CK_InvoiceLine_VatRate_Range", "[VatRate] BETWEEN 0 AND 100"));

            // Quantity, UnitPrice >= 0
            builder.ToTable(t => t.HasCheckConstraint("CK_InvoiceLine_Qty_Positive", "[Qty] >= 0"));
            builder.ToTable(t => t.HasCheckConstraint("CK_InvoiceLine_UnitPrice_Positive", "[UnitPrice] >= 0"));

        }
    }

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


}
