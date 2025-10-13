using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // --- Contacts ---
        if (!await db.Contacts.AnyAsync())
        {
            db.Contacts.AddRange(
                new Contact { Name = "ACME Ltd.", Type = ContactType.Customer, Email = "info@acme.local" },
                new Contact { Name = "Tedarikçi A.Ş.", Type = ContactType.Vendor, Email = "siparis@tedarikci.local" },
                new Contact { Name = "Globex A.Ş.", Type = ContactType.Customer, Email = "hello@globex.local" }
            );
        }

        // --- Items ---
        if (!await db.Items.AnyAsync())
        {
            db.Items.AddRange(
                new Item { Name = "Hizmet A", Unit = "saat", VatRate = 20, DefaultUnitPrice = 750.00m },
                new Item { Name = "Ürün B", Unit = "adet", VatRate = 1, DefaultUnitPrice = 50.00m },
                new Item { Name = "Danışmanlık", Unit = "saat", VatRate = 20, DefaultUnitPrice = 950.00m },
                new Item { Name = "Kargo", Unit = "adet", VatRate = 20, DefaultUnitPrice = 80.00m }
            );
        }

        // --- Cash / Bank Accounts ---
        if (!await db.CashBankAccounts.AnyAsync())
        {
            db.CashBankAccounts.AddRange(
                new CashBankAccount { Name = "Kasa", Type = CashBankAccountType.Cash },
                new CashBankAccount { Name = "Banka (TL)", Type = CashBankAccountType.Bank, Iban = "TR120006200000000123456789" }
            );
        }

        // Önce temel kayıtları kaydedelim ki Id'ler oluşsun
        await db.SaveChangesAsync();

        // Id'leri al
        var kasaId = await db.CashBankAccounts.Where(x => x.Name == "Kasa").Select(x => x.Id).FirstOrDefaultAsync();
        var bankaTlId = await db.CashBankAccounts.Where(x => x.Name == "Banka (TL)").Select(x => x.Id).FirstOrDefaultAsync();

        var acmeId = await db.Contacts.Where(x => x.Name == "ACME Ltd.").Select(x => x.Id).FirstOrDefaultAsync();
        var tedarikciId = await db.Contacts.Where(x => x.Name == "Tedarikçi A.Ş.").Select(x => x.Id).FirstOrDefaultAsync();
        var globexId = await db.Contacts.Where(x => x.Name == "Globex A.Ş.").Select(x => x.Id).FirstOrDefaultAsync();

        // --- Payments (TRY + USD; In/Out) ---
        if (!await db.Payments.AnyAsync())
        {
            var now = DateTime.UtcNow;

            db.Payments.AddRange(
                // Gelen (In) TRY – müşteri tahsilatı
                new Payment
                {
                    AccountId = kasaId,
                    ContactId = acmeId,
                    Direction = PaymentDirection.In,
                    Amount = 1500.00m,
                    Currency = "TRY",
                    DateUtc = now.AddDays(-2)
                },
                // Giden (Out) TRY – tedarikçiye ödeme
                new Payment
                {
                    AccountId = bankaTlId,
                    ContactId = tedarikciId,
                    Direction = PaymentDirection.Out,
                    Amount = 900.00m,
                    Currency = "TRY",
                    DateUtc = now.AddDays(-1).AddHours(-3)
                },
                // Gelen (In) USD – başka bir müşteriden tahsilat
                new Payment
                {
                    AccountId = bankaTlId,
                    ContactId = globexId,
                    Direction = PaymentDirection.In,
                    Amount = 200.00m,
                    Currency = "USD",
                    DateUtc = now.AddDays(-1).AddHours(2)
                },
                // Giden (Out) TRY – küçük masraf
                new Payment
                {
                    AccountId = kasaId,
                    ContactId = tedarikciId,
                    Direction = PaymentDirection.Out,
                    Amount = 120.00m,
                    Currency = "TRY",
                    DateUtc = now.AddHours(-6)
                }
            );
        }

        // (Opsiyonel) Demo amaçlı pasif/soft-deleted örneği istersen:
        // if (!await db.Contacts.AnyAsync(c => c.Name == "Silinmiş Demo"))
        // {
        //     db.Contacts.Add(new Contact { Name = "Silinmiş Demo", Type = ContactType.Customer, Email = "deleted@demo.local", IsDeleted = true, DeletedAtUtc = DateTime.UtcNow });
        // }

        await db.SaveChangesAsync();
    }
}
