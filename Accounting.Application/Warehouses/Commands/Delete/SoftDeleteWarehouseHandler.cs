using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Exceptions;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Warehouses.Commands.Delete;

public class SoftDeleteWarehouseHandler(IAppDbContext db)
    : IRequestHandler<SoftDeleteWarehouseCommand>
{
    public async Task Handle(SoftDeleteWarehouseCommand r, CancellationToken ct)
    {
        var e = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == r.Id, ct);
        if (e is null) throw new NotFoundException("Warehouse", r.Id);

        db.Entry(e).Property(nameof(Warehouse.RowVersion)).OriginalValue = Convert.FromBase64String(r.RowVersion);

        e.IsDeleted = true;
        e.DeletedAtUtc = DateTime.UtcNow;
        e.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Depo silinirken eşzamanlılık hatası oluştu.");
        }
    }
}
