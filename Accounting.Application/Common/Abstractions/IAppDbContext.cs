using Accounting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
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
    DbSet<ExpenseList> ExpenseLists { get; }
    DbSet<Expense> Expenses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    // concurrency için gerekli
    EntityEntry Entry(object entity);
}
