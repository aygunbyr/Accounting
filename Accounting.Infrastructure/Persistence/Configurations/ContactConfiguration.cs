using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence.Configurations; // ApplyRowVersion/ApplySoftDelete
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> b)
    {
        b.ToTable("Contacts");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code)
         .IsRequired()
         .HasMaxLength(32)
         .IsUnicode(true);

        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.TaxNo).HasMaxLength(20);
        b.Property(x => x.Email).HasMaxLength(320);
        b.Property(x => x.Phone).HasMaxLength(40);

        b.HasOne(c => c.Branch)
            .WithMany()
            .HasForeignKey(c => c.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        // audit
        b.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()")
            .ValueGeneratedOnAdd()
            .IsRequired();

        // concurrency + soft delete (tek merkezden)
        b.ApplyRowVersion();
        b.ApplySoftDelete();

        // indexes
        b.HasIndex(x => x.Name).HasDatabaseName("IX_Contacts_Name");
        b.HasIndex(x => x.Code)
            .HasDatabaseName("UX_Contacts_Code")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => x.Type);
        b.HasIndex(x => new { x.Type, x.Name });
        b.HasIndex(x => x.BranchId).HasDatabaseName("IX_Contacts_BranchId");

    }
}
