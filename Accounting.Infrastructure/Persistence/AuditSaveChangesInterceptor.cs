using Accounting.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Accounting.Infrastructure.Persistence.Interceptors;

public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplyAudit(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is IHasTimestamps t)
                {
                    if (t.CreatedAtUtc == default) t.CreatedAtUtc = now;
                    t.UpdatedAtUtc = null;
                }
                if (entry.Entity is ISoftDeletable sd)
                {
                    sd.IsDeleted = false;
                    sd.DeletedAtUtc = null;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is IHasTimestamps t)
                {
                    t.UpdatedAtUtc = now;
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                if (entry.Entity is ISoftDeletable sd)
                {
                    // soft delete’e çevir
                    entry.State = EntityState.Modified;
                    sd.IsDeleted = true;
                    sd.DeletedAtUtc = now;
                }
            }
        }
    }
}
