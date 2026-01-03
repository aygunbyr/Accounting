using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Utils;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Services;

public interface IInvoiceBalanceService
{
    Task<decimal> CalculateBalanceAsync(int invoiceId, CancellationToken ct = default);
    Task<decimal> RecalculateBalanceAsync(int invoiceId, CancellationToken ct = default);
}

public class InvoiceBalanceService : IInvoiceBalanceService
{
    private readonly IAppDbContext _db;
    public InvoiceBalanceService(IAppDbContext db) => _db = db;

    public async Task<decimal> CalculateBalanceAsync(int invoiceId, CancellationToken ct)
    {
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

    public async Task<decimal> RecalculateBalanceAsync(int invoiceId, CancellationToken ct)
    {
        var balance = await CalculateBalanceAsync(invoiceId, ct);

        var invoice = await _db.Invoices
            .Where(i => i.Id == invoiceId)
            .FirstOrDefaultAsync(ct);

        if (invoice != null)
        {
            invoice.Balance = balance;
        }

        return balance;
    }
}
