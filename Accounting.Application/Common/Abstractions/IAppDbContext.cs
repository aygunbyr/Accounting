using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage; // <-- EKLE
using System.Threading;
using System.Threading.Tasks;

namespace Accounting.Application.Common.Abstractions;

public interface IAppDbContext
{
    DbSet<Contact> Contacts { get; }
    DbSet<Item> Items { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLine> InvoiceLines { get; }
    DbSet<CashBankAccount> CashBankAccounts { get; }
    DbSet<Payment> Payments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Transaction desteği
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
