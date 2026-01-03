using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Utils;
using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Services;

/// <summary>
/// Invoice balance hesaplama ve güncelleme servisi.
/// Balance = TotalGross - SUM(Payments linked to this invoice)
/// 
/// CONCURRENCY SAFE: 
/// 1. Tracking kullanır (memory'deki Add'leri görür)
/// 2. UPDLOCK ile race condition'ları önler
/// </summary>
public interface IInvoiceBalanceService
{
    /// <summary>
    /// Belirtilen invoice'un güncel balance'ını hesaplar ve günceller.
    /// THREAD-SAFE: Invoice'u kilitler (UPDLOCK, HOLDLOCK)
    /// </summary>
    Task<decimal> RecalculateBalanceAsync(int invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Belirtilen invoice'un güncel balance'ını hesaplar (DB'ye yazmaz).
    /// Read-only - raporlama amaçlı kullanılabilir.
    /// </summary>
    Task<decimal> CalculateBalanceAsync(int invoiceId, CancellationToken ct = default);
}

public class InvoiceBalanceService : IInvoiceBalanceService
{
    private readonly IAppDbContext _db;

    public InvoiceBalanceService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> CalculateBalanceAsync(int invoiceId, CancellationToken ct = default)
    {
        // Read-only hesaplama (raporlar için)
        var invoice = await _db.Invoices
            .AsNoTracking()
            .Where(i => i.Id == invoiceId)
            .Select(i => new { i.TotalGross })
            .FirstOrDefaultAsync(ct);

        if (invoice == null)
            return 0m;

        var totalPayments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.LinkedInvoiceId == invoiceId && !p.IsDeleted)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        var balance = Money.R2(invoice.TotalGross - totalPayments);

        return balance;
    }

    public async Task<decimal> RecalculateBalanceAsync(int invoiceId, CancellationToken ct = default)
    {
        // IAppDbContext'i DbContext'e cast et (FromSqlInterpolated için gerekli)
        var dbContext = _db as DbContext;
        if (dbContext == null)
        {
            throw new InvalidOperationException(
                "IAppDbContext must be DbContext instance to use raw SQL queries for locking.");
        }

        // 🔒 PESSIMISTIC LOCK: Invoice'u kilitle
        // UPDLOCK: Update intent lock (başka transaction okuyabilir ama değiştiremez)
        // HOLDLOCK: Transaction bitene kadar lock'u tut
        // Bu sayede concurrent RecalculateBalance çağrıları sırayla çalışır

        var invoice = await _db.QueryRaw<Invoice>($@"
            SELECT * FROM Invoices WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = {invoiceId}
        ").FirstOrDefaultAsync(ct);

        if (invoice == null)
            return 0m;

        // ✅ Tracking kullan (AsNoTracking YOK!)
        // Memory'deki Add/Update'leri de görür
        // Örnek: _db.Payments.Add() yapılmış ama SaveChanges henüz çağrılmamışsa,
        // bu query onu da görür ve toplama dahil eder
        var totalPayments = await _db.Payments
            .Where(p => p.LinkedInvoiceId == invoiceId && !p.IsDeleted)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        var balance = Money.R2(invoice.TotalGross - totalPayments);
        invoice.Balance = balance;

        // SaveChanges caller tarafından yapılacak
        // Lock, SaveChanges + transaction commit olana kadar tutulur

        return balance;
    }
}