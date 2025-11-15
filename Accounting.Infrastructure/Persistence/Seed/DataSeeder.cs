using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // --- Helpers (rounding: AwayFromZero) ---
        static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        static decimal R3(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
        static decimal R4(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

        // ------------------------------------------------
        // 1) CONTACTS (10 adet)
        // ------------------------------------------------
        if (!await db.Contacts.AnyAsync())
        {
            var contacts = new List<Contact>();
            // 6 müşteri
            for (int i = 1; i <= 6; i++)
            {
                contacts.Add(new Contact
                {
                    Type = ContactType.Customer,
                    Code = $"CUST{i:000}",
                    Name = $"Müşteri {i}",
                    Email = $"musteri{i}@demo.local",
                    Phone = $"+90 212 000 0{i:000}"
                });
            }
            // 4 tedarikçi
            for (int i = 1; i <= 4; i++)
            {
                contacts.Add(new Contact
                {
                    Type = ContactType.Vendor,
                    Code = $"VEND{i:000}",
                    Name = $"Tedarikçi {i}",
                    Email = $"tedarikci{i}@demo.local",
                    Phone = $"+90 216 111 0{i:000}"
                });
            }
            db.Contacts.AddRange(contacts);
        }

        // ------------------------------------------------
        // 2) ITEMS (10 adet)
        // ------------------------------------------------
        if (!await db.Items.AnyAsync())
        {
            var units = new[] { "adet", "kg", "lt", "saat" };
            var items = new List<Item>();
            for (int i = 1; i <= 10; i++)
            {
                items.Add(new Item
                {
                    Code = $"ITEM{i:000}",
                    Name = $"Stok {i}",
                    Unit = units[(i - 1) % units.Length],
                    VatRate = (i % 5 == 0) ? 1 : 20, // arada 1% KDV
                    DefaultUnitPrice = R4(25m + i * 7.5m)
                });
            }
            db.Items.AddRange(items);
        }

        // ------------------------------------------------
        // 3) CASH/BANK ACCOUNTS (10 adet: 5 kasa, 5 banka)
        // ------------------------------------------------
        if (!await db.CashBankAccounts.AnyAsync())
        {
            var accs = new List<CashBankAccount>();
            for (int i = 1; i <= 5; i++)
            {
                accs.Add(new CashBankAccount
                {
                    Type = CashBankAccountType.Cash,
                    Code = $"CASH{i:000}",
                    Name = $"Kasa {i}",
                });
            }
            for (int i = 1; i <= 5; i++)
            {
                accs.Add(new CashBankAccount
                {
                    Type = CashBankAccountType.Bank,
                    Code = $"BANK{i:000}",
                    Name = $"Banka {i}",
                    Iban = $"TR{i:00}0006200000000{i:000000000}"
                });
            }
            db.CashBankAccounts.AddRange(accs);
        }

        // ------------------------------------------------
        // 4) EXPENSE DEFINITIONS (örnek masraf tipleri)  // CHANGED
        // ------------------------------------------------
        if (!await db.ExpenseDefinitions.AnyAsync()) // CHANGED
        {
            var defs = new List<ExpenseDefinition>    // CHANGED
            {
                new()
                {
                    Code = "YOL",
                    Name = "Yol / Ulaşım",
                    DefaultVatRate = 20,
                    IsActive = true
                },
                new()
                {
                    Code = "YEMEK",
                    Name = "Yemek / İkram",
                    DefaultVatRate = 10,
                    IsActive = true
                },
                new()
                {
                    Code = "KIRTASIYE",
                    Name = "Kırtasiye",
                    DefaultVatRate = 20,
                    IsActive = true
                },
                new()
                {
                    Code = "YAZILIMABO",
                    Name = "Yazılım Aboneliği",
                    DefaultVatRate = 20,
                    IsActive = true
                }
            };

            db.ExpenseDefinitions.AddRange(defs);    // CHANGED
        }

        // Önce temel kayıtları kaydedelim (ID’ler oluşsun)
        await db.SaveChangesAsync();

        // Lookup listeleri (IDs & entities)
        var contactIds = await db.Contacts.OrderBy(c => c.Id).Select(c => c.Id).ToListAsync();
        var customerIds = await db.Contacts.Where(c => c.Type == ContactType.Customer).OrderBy(c => c.Id).Select(c => c.Id).ToListAsync();
        var vendorIds = await db.Contacts.Where(c => c.Type == ContactType.Vendor).OrderBy(c => c.Id).Select(c => c.Id).ToListAsync();

        var itemsAll = await db.Items.AsNoTracking().OrderBy(i => i.Id).ToListAsync();
        var accountIds = await db.CashBankAccounts.OrderBy(a => a.Id).Select(a => a.Id).ToListAsync();

        var now = DateTime.UtcNow;

        // ------------------------------------------------
        // 5) INVOICES (iade dâhil)                       // CHANGED (sadece numara)
        //    - Satırlar hep pozitif
        //    - SalesReturn / PurchaseReturn ise header toplamları negatif
        // ------------------------------------------------
        if (!await db.Invoices.AnyAsync())
        {
            var invoices = new List<Invoice>();
            // 12 fatura (çeşitli türlerde)
            for (int i = 1; i <= 12; i++)
            {
                // Tür seçimi: biraz dağıtalım
                InvoiceType invType =
                    (i % 6 == 0) ? InvoiceType.SalesReturn :
                    (i % 5 == 0) ? InvoiceType.PurchaseReturn :
                    (i % 2 == 0) ? InvoiceType.Purchase :
                                   InvoiceType.Sales;

                // Contact seçimi: Sales/SalesReturn -> müşteri, Purchase/PurchaseReturn -> tedarikçi
                int contactId =
                    (invType == InvoiceType.Sales || invType == InvoiceType.SalesReturn)
                        ? customerIds[(i - 1) % customerIds.Count]
                        : vendorIds[(i - 1) % vendorIds.Count];

                // Item ve miktar/fiyat
                var item = itemsAll[(i - 1) % itemsAll.Count];
                var qty = R3(1m + (i % 3));                // 1..3
                var unitPrice = R4(item.DefaultUnitPrice ?? 50m);
                var vatRate = item.VatRate;

                // Satır tutarları (pozitif)
                var net = R2(qty * unitPrice);
                var vat = R2(net * vatRate / 100m);
                var gross = R2(net + vat);

                // Header işareti (iade ise -1)
                decimal sign = (invType == InvoiceType.SalesReturn || invType == InvoiceType.PurchaseReturn) ? -1m : 1m;

                var inv = new Invoice
                {
                    ContactId = contactId,
                    Type = invType,
                    DateUtc = now.AddDays(-i),
                    Currency = (i % 4 == 0) ? "USD" : "TRY",

                    // Header toplamları (iade negatif)
                    TotalNet = R2(sign * net),
                    TotalVat = R2(sign * vat),
                    TotalGross = R2(sign * gross),

                    Lines = new List<InvoiceLine>
                    {
                        new InvoiceLine
                        {
                            ItemId      = item.Id,
                            ItemCode    = item.Code,   // snapshot
                            ItemName    = item.Name,   // snapshot
                            Unit        = item.Unit,   // snapshot
                            Qty         = qty,
                            UnitPrice   = unitPrice,
                            VatRate     = vatRate,
                            Net         = net,         // satır hep pozitif
                            Vat         = vat,
                            Gross       = gross,
                            CreatedAtUtc= now.AddDays(-i)
                        }
                    },

                    CreatedAtUtc = now.AddDays(-i)
                };

                invoices.Add(inv);
            }

            db.Invoices.AddRange(invoices);
        }

        // ------------------------------------------------
        // 6) PAYMENTS (10 adet)                          // CHANGED (sadece numara)
        // ------------------------------------------------
        if (!await db.Payments.AnyAsync())
        {
            var payments = new List<Payment>();
            for (int i = 1; i <= 10; i++)
            {
                var accountId = accountIds[(i - 1) % accountIds.Count];
                var contactId = contactIds[(i - 1) % contactIds.Count];

                payments.Add(new Payment
                {
                    AccountId = accountId,
                    ContactId = contactId,
                    Direction = (i % 2 == 0) ? PaymentDirection.In : PaymentDirection.Out,
                    Amount = R2(100m + i * 37.25m),
                    Currency = (i % 3 == 0) ? "USD" : "TRY",
                    DateUtc = now.AddHours(-i * 6),
                    CreatedAtUtc = now.AddHours(-i * 6)
                });
            }
            db.Payments.AddRange(payments);
        }

        // ------------------------------------------------
        // 7) EXPENSE LISTS (10 adet) + EXPENSES (10 adet, her listeye 1 adet) // CHANGED (sadece numara)
        // ------------------------------------------------
        if (!await db.ExpenseLists.AnyAsync())
        {
            var lists = new List<ExpenseList>();
            for (int i = 1; i <= 10; i++)
            {
                var list = new ExpenseList
                {
                    Name = $"Masraf Listesi {i}",
                    Status = (i % 3 == 0) ? ExpenseListStatus.Reviewed : ExpenseListStatus.Draft,
                    CreatedAtUtc = now.AddDays(-i),
                };

                // Her listeye 1 masraf
                var supplierId = vendorIds[(i - 1) % vendorIds.Count];
                var amount = R2(50m + i * 12.4m);
                var vatRate = (i % 5 == 0) ? 1 : 20;

                list.Lines.Add(new Expense
                {
                    DateUtc = now.AddDays(-i).AddHours(-2),
                    SupplierId = supplierId,
                    Currency = (i % 4 == 0) ? "USD" : "TRY",
                    Amount = amount,
                    VatRate = vatRate,
                    Category = (i % 2 == 0) ? "Ulaşım" : "Kırtasiye",
                    Notes = (i % 3 == 0) ? "Toplantı gideri" : null,
                    CreatedAtUtc = now.AddDays(-i).AddHours(-2)
                });

                lists.Add(list);
            }

            db.ExpenseLists.AddRange(lists);
        }

        // ------------------------------------------------
        // KAYDET
        // ------------------------------------------------
        await db.SaveChangesAsync();
    }
}
