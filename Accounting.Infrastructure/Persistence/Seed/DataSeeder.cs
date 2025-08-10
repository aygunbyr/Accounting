using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Contacts.AnyAsync())
        {
            db.Contacts.AddRange(
                new Contact { Name = "ACME Ltd.", Type = ContactType.Customer, Email = "info@acme.local" },
                new Contact { Name = "Tedarikçi A.Ş.", Type = ContactType.Vendor }
            );
        }
        if (!await db.Items.AnyAsync())
        {
            db.Items.AddRange(
                new Item { Name = "Hizmet A", Unit = "saat", VatRate = 20, DefaultUnitPrice = 750.0000m },
                new Item { Name = "Ürün B", Unit = "adet", VatRate = 1, DefaultUnitPrice = 50.0000m }
            );
        }
        if (!await db.CashBankAccounts.AnyAsync())
        {
            db.CashBankAccounts.Add(new CashBankAccount { Name = "Kasa", Type = CashBankAccountType.Cash });
        }

        await db.SaveChangesAsync();
    }
}
