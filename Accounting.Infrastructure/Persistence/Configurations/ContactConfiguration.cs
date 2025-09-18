using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> b)
    {
        b.ToTable("Contacts");

        b.HasKey(x => x.Id);

        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.TaxNo).HasMaxLength(20);
        b.Property(x => x.Email).HasMaxLength(320);
        b.Property(x => x.Phone).HasMaxLength(40);

        b.HasIndex(x => x.Name);
        b.HasIndex(x => x.Type);

        b.Property(x => x.RowVersion).IsRowVersion(); // optimistic concurrency
        b.HasQueryFilter(x => !x.IsDeleted); // soft delete
        b.HasIndex(x => new { x.Type, x.Name }); // basit arama için
    }
}
