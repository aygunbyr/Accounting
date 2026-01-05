using Accounting.Application.Services;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, IInvoiceBalanceService balanceService, CancellationToken ct = default)
    {
        // Helpers (AwayFromZero)
        static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        static decimal R3(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
        static decimal R4(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);

        // 1) Branches
        await SeedBranchesAsync(db, ct);
        var branchIds = await GetActiveBranchIdsAsync(db, ct);

        // 2) Warehouses (per branch default)
        await SeedWarehousesAsync(db, branchIds, ct);
        var warehousesAll = await GetActiveWarehousesAsync(db, ct);
        var defaultWarehouseByBranch = BuildDefaultWarehouseByBranch(warehousesAll);

        // 3) Contacts (Customer/Vendor/Employee)
        await SeedContactsAsync(db, branchIds, ct);

        // 4) Items
        await SeedItemsAsync(db, branchIds, R4, ct);

        // 5) Cash/Bank Accounts
        await SeedCashBankAccountsAsync(db, branchIds, ct);

        // 6) ExpenseDefinitions
        await SeedExpenseDefinitionsAsync(db, ct);

        // Lookup’lar (seed sonrası)
        var contactIds = await db.Contacts.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(ct);
        var customerIds = await db.Contacts.AsNoTracking().Where(x => x.Type == ContactType.Customer).OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(ct);
        var vendorIds = await db.Contacts.AsNoTracking().Where(x => x.Type == ContactType.Vendor).OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(ct);
        var employeeIds = await db.Contacts.AsNoTracking().Where(x => x.Type == ContactType.Employee).OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(ct);

        var itemsAll = await db.Items.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.Id).ToListAsync(ct);
        var accountIds = await db.CashBankAccounts.AsNoTracking().Where(x => !x.IsDeleted).OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(ct);

        var now = DateTime.UtcNow;

        // 7) Stock MVP demo (movements + stocks) — istersen burada dursun (sen önce SRP istedin, SRP method yaptım)
        await SeedStockMovementsAndStocksAsync(
            db,
            branchIds,
            itemsAll,
            defaultWarehouseByBranch,
            now,
            R3,
            ct);

        // 8) Invoices
        await SeedInvoicesAsync(db, branchIds, itemsAll, customerIds, vendorIds, now, R2, R3, R4, ct);

        // 9) Payments + balance recalc
        await SeedPaymentsAsync(db, branchIds, contactIds, accountIds, balanceService, now, R2, ct);

        // 10) ExpenseLists
        await SeedExpenseListsAsync(db, branchIds, vendorIds, now, R2, ct);

        // 11) FixedAssets
        await SeedFixedAssetsAsync(db, branchIds, now, R2, R4, ct);

        await db.SaveChangesAsync(ct);
    }

    // -----------------------------
    // SRP METHODS
    // -----------------------------

    private static async Task SeedBranchesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Branches.AnyAsync(ct)) return;

        var now = DateTime.UtcNow;

        db.Branches.AddRange(new List<Branch>
        {
            new() { Code = "MERKEZ", Name = "Merkez Şube", CreatedAtUtc = now },
            new() { Code = "ANKARA", Name = "Ankara Şubesi", CreatedAtUtc = now },
            new() { Code = "IZMIR",  Name = "İzmir Şubesi",  CreatedAtUtc = now }
        });

        await db.SaveChangesAsync(ct);
    }

    private static async Task<List<int>> GetActiveBranchIdsAsync(AppDbContext db, CancellationToken ct)
    {
        var ids = await db.Branches
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToListAsync(ct);

        // fallback
        return ids.Count > 0 ? ids : new List<int> { 1 };
    }

    private static async Task SeedWarehousesAsync(AppDbContext db, List<int> branchIds, CancellationToken ct)
    {
        // “Hiç warehouse yoksa” basit seed.
        // İstersen bir sonraki adımda “şube bazlı DEPO yoksa ekle” şeklinde iyileştiririz.
        if (await db.Warehouses.AnyAsync(ct)) return;

        var now = DateTime.UtcNow;

        var list = new List<Warehouse>();
        foreach (var branchId in branchIds)
        {
            list.Add(new Warehouse
            {
                BranchId = branchId,
                Code = "DEPO",
                Name = "Ana Depo",
                IsDefault = true,
                CreatedAtUtc = now
            });
        }

        db.Warehouses.AddRange(list);
        await db.SaveChangesAsync(ct);
    }

    private static async Task<List<Warehouse>> GetActiveWarehousesAsync(AppDbContext db, CancellationToken ct)
    {
        return await db.Warehouses
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
    }

    private static Dictionary<int, Warehouse> BuildDefaultWarehouseByBranch(List<Warehouse> warehousesAll)
    {
        return warehousesAll
            .GroupBy(w => w.BranchId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.IsDefault).ThenBy(x => x.Id).First()
            );
    }

    private static async Task SeedContactsAsync(AppDbContext db, List<int> branchIds, CancellationToken ct)
    {
        if (await db.Contacts.AnyAsync(ct)) return;

        var contacts = new List<Contact>();

        // 6 müşteri
        for (int i = 1; i <= 6; i++)
        {
            contacts.Add(new Contact
            {
                BranchId = branchIds[(i - 1) % branchIds.Count],
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
                BranchId = branchIds[(i - 1) % branchIds.Count],
                Type = ContactType.Vendor,
                Code = $"VEND{i:000}",
                Name = $"Tedarikçi {i}",
                Email = $"tedarikci{i}@demo.local",
                Phone = $"+90 216 111 0{i:000}"
            });
        }

        // ✅ 5 çalışan/personel
        for (int i = 1; i <= 5; i++)
        {
            contacts.Add(new Contact
            {
                BranchId = branchIds[(i - 1) % branchIds.Count],
                Type = ContactType.Employee,
                Code = $"EMP{i:000}",
                Name = $"Çalışan {i}",
                Email = $"calisan{i}@demo.local",
                Phone = $"+90 532 222 0{i:000}"
            });
        }

        db.Contacts.AddRange(contacts);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedItemsAsync(AppDbContext db, List<int> branchIds, Func<decimal, decimal> R4, CancellationToken ct)
    {
        if (await db.Items.AnyAsync(ct)) return;

        var units = new[] { "adet", "kg", "lt", "saat" };
        var items = new List<Item>();

        for (int i = 1; i <= 10; i++)
        {
            items.Add(new Item
            {
                BranchId = branchIds[(i - 1) % branchIds.Count],
                Code = $"ITEM{i:000}",
                Name = $"Stok {i}",
                Unit = units[(i - 1) % units.Length],
                VatRate = (i % 5 == 0) ? 1 : 20,
                DefaultUnitPrice = R4(25m + i * 7.5m)
            });
        }

        db.Items.AddRange(items);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCashBankAccountsAsync(AppDbContext db, List<int> branchIds, CancellationToken ct)
    {
        if (await db.CashBankAccounts.AnyAsync(ct)) return;

        var accs = new List<CashBankAccount>();

        for (int i = 1; i <= 5; i++)
        {
            accs.Add(new CashBankAccount
            {
                BranchId = branchIds[(i - 1) % branchIds.Count],
                Type = CashBankAccountType.Cash,
                Code = $"CASH{i:000}",
                Name = $"Kasa {i}",
            });
        }

        for (int i = 1; i <= 5; i++)
        {
            accs.Add(new CashBankAccount
            {
                BranchId = branchIds[(i - 1) % branchIds.Count],
                Type = CashBankAccountType.Bank,
                Code = $"BANK{i:000}",
                Name = $"Banka {i}",
                Iban = $"TR{i:00}0006200000000{i:000000000}"
            });
        }

        db.CashBankAccounts.AddRange(accs);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedExpenseDefinitionsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.ExpenseDefinitions.AnyAsync(ct)) return;

        db.ExpenseDefinitions.AddRange(new List<ExpenseDefinition>
        {
            new() { Code = "YOL",      Name = "Yol / Ulaşım",        DefaultVatRate = 20, IsActive = true },
            new() { Code = "YEMEK",    Name = "Yemek / İkram",       DefaultVatRate = 10, IsActive = true },
            new() { Code = "KIRTASIYE",Name = "Kırtasiye",           DefaultVatRate = 20, IsActive = true },
            new() { Code = "YAZILIMABO",Name = "Yazılım Aboneliği",  DefaultVatRate = 20, IsActive = true }
        });

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedStockMovementsAndStocksAsync(
        AppDbContext db,
        List<int> branchIds,
        List<Item> itemsAll,
        Dictionary<int, Warehouse> defaultWarehouseByBranch,
        DateTime now,
        Func<decimal, decimal> R3,
        CancellationToken ct)
    {
        // StockMovements
        if (!await db.StockMovements.AnyAsync(ct))
        {
            var itemsByBranch = itemsAll
                .GroupBy(i => i.BranchId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).ToList());

            var movements = new List<StockMovement>();
            var stockMap = new Dictionary<(int BranchId, int WarehouseId, int ItemId), decimal>();

            foreach (var branchId in branchIds)
            {
                if (!defaultWarehouseByBranch.TryGetValue(branchId, out var wh))
                    continue;

                if (!itemsByBranch.TryGetValue(branchId, out var branchItems) || branchItems.Count == 0)
                    continue;

                var takeCount = Math.Min(5, branchItems.Count);
                for (int j = 0; j < takeCount; j++)
                {
                    var item = branchItems[j];
                    var key = (branchId, wh.Id, item.Id);

                    // PurchaseIn
                    var inQty = R3(10m + (j * 3m));
                    movements.Add(new StockMovement
                    {
                        BranchId = branchId,
                        WarehouseId = wh.Id,
                        ItemId = item.Id,
                        Type = StockMovementType.PurchaseIn,
                        Quantity = inQty,
                        TransactionDateUtc = now.AddDays(-(j + 10)),
                        Note = "Demo: Alış girişi",
                        CreatedAtUtc = now.AddDays(-(j + 10))
                    });

                    stockMap[key] = R3((stockMap.TryGetValue(key, out var cur) ? cur : 0m) + inQty);

                    // SalesOut (bazı item’larda)
                    if (j % 2 == 0)
                    {
                        var outQty = R3(2m + j);
                        var available = stockMap[key];
                        if (outQty > available) outQty = available;

                        if (outQty > 0m)
                        {
                            movements.Add(new StockMovement
                            {
                                BranchId = branchId,
                                WarehouseId = wh.Id,
                                ItemId = item.Id,
                                Type = StockMovementType.SalesOut,
                                Quantity = outQty,
                                TransactionDateUtc = now.AddDays(-(j + 5)),
                                Note = "Demo: Satış çıkışı",
                                CreatedAtUtc = now.AddDays(-(j + 5))
                            });

                            stockMap[key] = R3(stockMap[key] - outQty);
                        }
                    }
                }
            }

            db.StockMovements.AddRange(movements);
            await db.SaveChangesAsync(ct);
        }

        // Stocks snapshot
        if (!await db.Stocks.AnyAsync(ct))
        {
            var mv = await db.StockMovements
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Select(x => new { x.BranchId, x.WarehouseId, x.ItemId, x.Type, x.Quantity })
                .ToListAsync(ct);

            var map = new Dictionary<(int BranchId, int WarehouseId, int ItemId), decimal>();

            foreach (var m in mv)
            {
                var key = (m.BranchId, m.WarehouseId, m.ItemId);
                var signed = (m.Type == StockMovementType.PurchaseIn || m.Type == StockMovementType.AdjustmentIn)
                    ? m.Quantity
                    : -m.Quantity;

                map[key] = R3((map.TryGetValue(key, out var cur) ? cur : 0m) + signed);
            }

            var stocks = map.Select(kvp => new Stock
            {
                BranchId = kvp.Key.BranchId,
                WarehouseId = kvp.Key.WarehouseId,
                ItemId = kvp.Key.ItemId,
                Quantity = kvp.Value,
                CreatedAtUtc = now
            }).ToList();

            db.Stocks.AddRange(stocks);
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedInvoicesAsync(
        AppDbContext db,
        List<int> branchIds,
        List<Item> itemsAll,
        List<int> customerIds,
        List<int> vendorIds,
        DateTime now,
        Func<decimal, decimal> R2,
        Func<decimal, decimal> R3,
        Func<decimal, decimal> R4,
        CancellationToken ct)
    {
        if (await db.Invoices.AnyAsync(ct)) return;

        var invoices = new List<Invoice>();
        var effectiveBranchIds = branchIds.Count > 0 ? branchIds : new List<int> { 1 };

        for (int i = 1; i <= 12; i++)
        {
            var invType =
                (i % 6 == 0) ? InvoiceType.SalesReturn :
                (i % 5 == 0) ? InvoiceType.PurchaseReturn :
                (i % 2 == 0) ? InvoiceType.Purchase :
                               InvoiceType.Sales;

            var contactId =
                (invType == InvoiceType.Sales || invType == InvoiceType.SalesReturn)
                    ? customerIds[(i - 1) % customerIds.Count]
                    : vendorIds[(i - 1) % vendorIds.Count];

            var item = itemsAll[(i - 1) % itemsAll.Count];
            var qty = R3(1m + (i % 3));
            var unitPrice = R4(item.DefaultUnitPrice ?? 50m);
            var vatRate = item.VatRate;

            var net = R2(qty * unitPrice);
            var vat = R2(net * vatRate / 100m);
            var gross = R2(net + vat);

            var branchId = effectiveBranchIds[(i - 1) % effectiveBranchIds.Count];

            invoices.Add(new Invoice
            {
                BranchId = branchId,
                ContactId = contactId,
                Type = invType,
                DateUtc = now.AddDays(-i),
                Currency = (i % 4 == 0) ? "USD" : "TRY",
                TotalNet = R2(net),
                TotalVat = R2(vat),
                TotalGross = R2(gross),
                Balance = R2(gross),
                Lines = new List<InvoiceLine>
                {
                    new()
                    {
                        ItemId = item.Id,
                        ItemCode = item.Code,
                        ItemName = item.Name,
                        Unit = item.Unit,
                        Qty = qty, 
                        UnitPrice = unitPrice,
                        VatRate = vatRate,
                        Net = net,
                        Vat = vat,
                        Gross = gross,
                        CreatedAtUtc = now.AddDays(-i)
                    }
                },
                CreatedAtUtc = now.AddDays(-i)
            });
        }

        db.Invoices.AddRange(invoices);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedPaymentsAsync(
        AppDbContext db,
        List<int> branchIds,
        List<int> contactIds,
        List<int> accountIds,
        IInvoiceBalanceService balanceService,
        DateTime now,
        Func<decimal, decimal> R2,
        CancellationToken ct)
    {
        if (await db.Payments.AnyAsync(ct)) return;

        var invoiceIds = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(ct);

        var payments = new List<Payment>();

        for (int i = 1; i <= Math.Min(10, invoiceIds.Count); i++)
        {
            var invoiceId = invoiceIds[i - 1];

            var invoice = await db.Invoices
                .AsNoTracking()
                .Where(inv => inv.Id == invoiceId)
                .Select(inv => new { inv.TotalGross, inv.Currency, inv.Type })
                .FirstOrDefaultAsync(ct);

            if (invoice == null) continue;
            if (invoice.Type != InvoiceType.Sales && invoice.Type != InvoiceType.Purchase) continue;
            if (invoice.TotalGross <= 0) continue;
            if (invoice.Currency != "TRY") continue;

            var percentage = 0.3m + ((i * 7) % 41) / 100m;
            var paymentAmount = R2(invoice.TotalGross * percentage);

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

        for (int i = 1; i <= 5; i++)
        {
            var accountId = accountIds[(i - 1) % accountIds.Count];
            var contactId = contactIds[(i - 1) % contactIds.Count];

            payments.Add(new Payment
            {
                BranchId = branchIds[(i - 1) % branchIds.Count],
                AccountId = accountId,
                ContactId = contactId,
                LinkedInvoiceId = null,
                Direction = (i % 2 == 0) ? PaymentDirection.In : PaymentDirection.Out,
                Amount = R2(100m + i * 50m),
                Currency = (i % 3 == 0) ? "USD" : "TRY",
                DateUtc = now.AddHours(-i * 12),
                CreatedAtUtc = now.AddHours(-i * 12)
            });
        }

        db.Payments.AddRange(payments);
        await db.SaveChangesAsync(ct);

        // balance recalc (linked)
        var linkedInvoiceIds = payments
            .Where(p => p.LinkedInvoiceId.HasValue)
            .Select(p => p.LinkedInvoiceId!.Value)
            .Distinct()
            .ToList();

        foreach (var invoiceId in linkedInvoiceIds)
        {
            await balanceService.RecalculateBalanceAsync(invoiceId);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedExpenseListsAsync(
        AppDbContext db,
        List<int> branchIds,
        List<int> vendorIds,
        DateTime now,
        Func<decimal, decimal> R2,
        CancellationToken ct)
    {
        if (await db.ExpenseLists.AnyAsync(ct)) return;

        var lists = new List<ExpenseList>();

        for (int i = 1; i <= 10; i++)
        {
            var list = new ExpenseList
            {
                BranchId = branchIds[(i - 1) % branchIds.Count],
                Name = $"Masraf Listesi {i}",
                Status = (i % 3 == 0) ? ExpenseListStatus.Reviewed : ExpenseListStatus.Draft,
                CreatedAtUtc = now.AddDays(-i),
            };

            var supplierId = vendorIds[(i - 1) % vendorIds.Count];
            var amount = R2(50m + i * 12.4m);
            var vatRate = (i % 5 == 0) ? 1 : 20;

            list.Lines.Add(new ExpenseLine
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
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedFixedAssetsAsync(
        AppDbContext db,
        List<int> branchIds,
        DateTime now,
        Func<decimal, decimal> R2,
        Func<decimal, decimal> R4,
        CancellationToken ct)
    {
        if (await db.FixedAssets.AnyAsync(ct)) return;

        var assets = new List<FixedAsset>
        {
            new()
            {
                BranchId = branchIds[0 % branchIds.Count],
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
                BranchId = branchIds[1 % branchIds.Count],
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
                BranchId = branchIds[2 % branchIds.Count],
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
        await db.SaveChangesAsync(ct);
    }
}
