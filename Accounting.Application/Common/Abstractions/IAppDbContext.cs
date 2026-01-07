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
    DbSet<Category> Categories { get; }
    DbSet<Item> Items { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLine> InvoiceLines { get; }
    DbSet<CashBankAccount> CashBankAccounts { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderLine> OrderLines { get; }
    DbSet<ExpenseList> ExpenseLists { get; }
    DbSet<ExpenseLine> ExpenseLines { get; }
    DbSet<ExpenseDefinition> ExpenseDefinitions { get; }
    DbSet<FixedAsset> FixedAssets { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<Stock> Stocks { get; }
    DbSet<StockMovement> StockMovements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    // concurrency için gerekli
    EntityEntry Entry(object entity);

    // raw sql query için gerekli
    IQueryable<T> QueryRaw<T>(FormattableString sql) where T : class;

}
