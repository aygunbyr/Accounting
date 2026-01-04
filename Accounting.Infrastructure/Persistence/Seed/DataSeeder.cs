using Accounting.Domain.Entities;
using Accounting.Application.Services;  // ✅ EKLE
using Microsoft.EntityFrameworkCore;
namespace Accounting.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, IInvoiceBalanceService balanceService)  // ✅ DEĞİŞTİ
    {
        // --- Helpers (rounding: AwayFromZero) ---
        static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        static decimal R3(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
        static decimal R4(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

        // ------------------------------------------------
        // 0) BRANCHES (örnek şubeler)
        // ------------------------------------------------
        if (!await db.Branches.AnyAsync())
        {
            var nowBranch = DateTime.UtcNow;

            var branches = new List<Branch>
            {
                new()
                {
                    Code = "MERKEZ",
                    Name = "Merkez Şube",
                    CreatedAtUtc = nowBranch
                },
                new()
                {
                    Code = "ANKARA",
                    Name = "Ankara Şubesi",
                    CreatedAtUtc = nowBranch
                },
                new()
                {
                    Code = "IZMIR",
                    Name = "İzmir Şubesi",
                    CreatedAtUtc = nowBranch
                }
            };

            db.Branches.AddRange(branches);
            await db.SaveChangesAsync();  // ✅ Branch'leri kaydet ki ID'ler oluşsun
        }

        // ✅ Branch ID'lerini hemen çek (Contact, Item, vb için gerekli)
        var branchIds = await db.Branches
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .Select(b => b.Id)
            .ToListAsync();

        // Eğer hiç branch yoksa fallback
        if (branchIds.Count == 0)
        {
            branchIds = new List<int> { 1 };  // Fallback
        }

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
                    BranchId = branchIds[(i - 1) % branchIds.Count],  // ✅ EKLE
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
                    BranchId = branchIds[(i - 1) % branchIds.Count],  // ✅ EKLE
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
                    BranchId = branchIds[(i - 1) % branchIds.Count],  // ✅ EKLE
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
                    BranchId = branchIds[(i - 1) % branchIds.Count],  // ✅ EKLE
                    Type = CashBankAccountType.Cash,
                    Code = $"CASH{i:000}",
                    Name = $"Kasa {i}",
                });
            }
            for (int i = 1; i <= 5; i++)
            {
                accs.Add(new CashBankAccount
                {
                    BranchId = branchIds[(i - 1) % branchIds.Count],  // ✅ EKLE
                    Type = CashBankAccountType.Bank,
                    Code = $"BANK{i:000}",
                    Name = $"Banka {i}",
                    Iban = $"TR{i:00}0006200000000{i:000000000}"
                });
            }
            db.CashBankAccounts.AddRange(accs);
        }

        // ------------------------------------------------
        // 4) EXPENSE DEFINITIONS (örnek masraf tipleri)
        // ------------------------------------------------
        if (!await db.ExpenseDefinitions.AnyAsync())
        {
            var defs = new List<ExpenseDefinition>
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

            db.ExpenseDefinitions.AddRange(defs);
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
        // 5) INVOICES (iade dâhil)
        //    - Satırlar hep pozitif
        //    - SalesReturn / PurchaseReturn ise header toplamları negatif
        //    - BranchId: mevcut şubeler arasında dağıtılır
        // ------------------------------------------------
        if (!await db.Invoices.AnyAsync())
        {
            var invoices = new List<Invoice>();

            // branch yoksa bile en azından 1 Id’lik fallback
            var effectiveBranchIds = branchIds.Count > 0
                ? branchIds
                : new List<int> { 1 };

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

                // 👇 BranchId: round-robin (1. fatura 1. şube, 2. fatura 2. şube, ...)
                var branchId = effectiveBranchIds[(i - 1) % effectiveBranchIds.Count];

                var inv = new Invoice
                {
                    BranchId = branchId,   // 👈 NEW

                    ContactId = contactId,
                    Type = invType,
                    DateUtc = now.AddDays(-i),
                    Currency = (i % 4 == 0) ? "USD" : "TRY",

                    // Header toplamları (iade negatif)
                    TotalNet = R2(sign * net),
                    TotalVat = R2(sign * vat),
                    TotalGross = R2(sign * gross),
                    Balance = R2(sign * gross),  // ✅ Başlangıç: henüz ödeme yok

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
            await db.SaveChangesAsync();  // ✅ Invoice ID'leri oluşsun
        }

        // ✅ Invoice ID'lerini çek (Payment linking için)
        var invoiceIds = await db.Invoices
            .AsNoTracking()
            .OrderBy(inv => inv.Id)
            .Select(inv => inv.Id)
            .ToListAsync();

        // ------------------------------------------------
        // 6) PAYMENTS (Invoice'lara bağlı + faturasız)
        // ------------------------------------------------
        if (!await db.Payments.AnyAsync())
        {
            var payments = new List<Payment>();

            // İlk 10 invoice'a kısmi ödemeler (TotalGross'u aşmayan)
            for (int i = 1; i <= Math.Min(10, invoiceIds.Count); i++)
            {
                var invoiceId = invoiceIds[i - 1];

                // Invoice bilgilerini al
                var invoice = await db.Invoices
                    .AsNoTracking()
                    .Where(inv => inv.Id == invoiceId)
                    .Select(inv => new { inv.TotalGross, inv.Currency, inv.Type })
                    .FirstOrDefaultAsync();

                if (invoice == null) continue;

                // Sadece Sales ve Purchase için ödeme (Return'lere ödeme yok)
                if (invoice.Type != InvoiceType.Sales && invoice.Type != InvoiceType.Purchase)
                    continue;

                // TotalGross negatifse atla (Return invoice'ları)
                if (invoice.TotalGross <= 0)
                    continue;

                // Currency TRY değilse atla (basitlik için)
                if (invoice.Currency != "TRY")
                    continue;

                // Ödeme tutarı: TotalGross'un %30-70 arası (rastgele)
                var percentage = 0.3m + ((i * 7) % 41) / 100m;  // 0.30-0.70 arası
                var paymentAmount = R2(invoice.TotalGross * percentage);

                // Account ve Contact seç
                var accountId = accountIds[(i - 1) % accountIds.Count];
                var contactId = contactIds[(i - 1) % contactIds.Count];

                payments.Add(new Payment
                {
                    BranchId = branchIds[(i - 1) % branchIds.Count],
                    AccountId = accountId,
                    ContactId = contactId,
                    LinkedInvoiceId = invoiceId,
                    Direction = invoice.Type == InvoiceType.Sales ? PaymentDirection.In : PaymentDirection.Out,
                    Amount = paymentAmount,
                    Currency = "TRY",
                    DateUtc = now.AddHours(-i * 6),
                    CreatedAtUtc = now.AddHours(-i * 6)
                });
            }

            // 5 faturasız ödeme (avans, genel giderler)
            for (int i = 1; i <= 5; i++)
            {
                var accountId = accountIds[(i - 1) % accountIds.Count];
                var contactId = contactIds[(i - 1) % contactIds.Count];

                payments.Add(new Payment
                {
                    BranchId = branchIds[(i - 1) % branchIds.Count],
                    AccountId = accountId,
                    ContactId = contactId,
                    LinkedInvoiceId = null,  // Faturasız
                    Direction = (i % 2 == 0) ? PaymentDirection.In : PaymentDirection.Out,
                    Amount = R2(100m + i * 50m),
                    Currency = (i % 3 == 0) ? "USD" : "TRY",
                    DateUtc = now.AddHours(-i * 12),
                    CreatedAtUtc = now.AddHours(-i * 12)
                });
            }

            db.Payments.AddRange(payments);
            await db.SaveChangesAsync();

            // Balance'ları güncelle (sadece linked payment'lar için)
            var linkedInvoiceIds = payments
                .Where(p => p.LinkedInvoiceId.HasValue)
                .Select(p => p.LinkedInvoiceId!.Value)
                .Distinct();

            foreach (var invoiceId in linkedInvoiceIds)
            {
                await balanceService.RecalculateBalanceAsync(invoiceId);
            }

            await db.SaveChangesAsync();
        }
        // 7) EXPENSE LISTS (10 adet) + EXPENSES (10 adet, her listeye 1 adet)
        // ------------------------------------------------
        // 7) EXPENSE LISTS (10 adet) + EXPENSES (10 adet, her listeye 1 adet)
        if (!await db.ExpenseLists.AnyAsync())
        {
            var lists = new List<ExpenseList>();
            for (int i = 1; i <= 10; i++)
            {
                var list = new ExpenseList
                {
                    // 👇 Şube id: round-robin
                    BranchId = branchIds[(i - 1) % branchIds.Count],

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
        // 8) FIXED ASSETS (demo demirbaşlar)
        // ------------------------------------------------
        if (!await db.FixedAssets.AnyAsync())
        {
            var assets = new List<FixedAsset>
            {
                new()
                {
                    BranchId = branchIds[0 % branchIds.Count],  // ✅ EKLE
                    Code = "DMR001",
                    Name = "Ofis Bilgisayarı",
                    PurchaseDateUtc = now.AddMonths(-18),
                    PurchasePrice = R2(25000m),
                    UsefulLifeYears = 5,
                    DepreciationRatePercent = R4(100m / 5m),
                    CreatedAtUtc = now.AddMonths(-18)
                },
                new()
                {
                    BranchId = branchIds[1 % branchIds.Count],  // ✅ EKLE
                    Code = "DMR002",
                    Name = "Ofis Mobilyası",
                    PurchaseDateUtc = now.AddMonths(-30),
                    PurchasePrice = R2(40000m),
                    UsefulLifeYears = 8,
                    DepreciationRatePercent = R4(100m / 8m),
                    CreatedAtUtc = now.AddMonths(-30)
                },
                new()
                {
                    BranchId = branchIds[2 % branchIds.Count],  // ✅ EKLE
                    Code = "DMR003",
                    Name = "Yazıcı ve Çevre Donanımı",
                    PurchaseDateUtc = now.AddMonths(-6),
                    PurchasePrice = R2(12000m),
                    UsefulLifeYears = 4,
                    DepreciationRatePercent = R4(100m / 4m),
                    CreatedAtUtc = now.AddMonths(-6)
                }
            };

            db.FixedAssets.AddRange(assets);
        }

        // ------------------------------------------------
        // KAYDET
        // ------------------------------------------------
        await db.SaveChangesAsync();
    }
}