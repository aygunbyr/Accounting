using Accounting.Application.Common.Abstractions;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace Accounting.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly AuditSaveChangesInterceptor _audit;

    public AppDbContext(DbContextOptions<AppDbContext> options, AuditSaveChangesInterceptor audit) : base(options)
    {
        _audit = audit;
    }

    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<CashBankAccount> CashBankAccounts => Set<CashBankAccount>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ExpenseList> ExpenseLists => Set<ExpenseList>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseDefinition> ExpenseDefinitions { get; set; } = null!;
    public DbSet<FixedAsset> FixedAssets { get; set; } = null!;
    public DbSet<Branch> Branches => Set<Branch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(_audit);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    => await Database.BeginTransactionAsync(cancellationToken);

    public new EntityEntry Entry(object entity) => base.Entry(entity);

}
