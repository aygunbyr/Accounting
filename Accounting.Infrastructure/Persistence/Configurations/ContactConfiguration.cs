using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounting.Infrastructure.Persistence.Configurations;

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
