using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Orders.Commands.Cancel;

public record CancelOrderCommand(int Id, string RowVersion) : IRequest<bool>, ITransactionalRequest;

public class CancelOrderHandler(IAppDbContext db) : IRequestHandler<CancelOrderCommand, bool>
{
    public async Task<bool> Handle(CancelOrderCommand r, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == r.Id && !o.IsDeleted, ct);
        if (order is null) throw new NotFoundException("Order", r.Id);

        if (order.Status == OrderStatus.Invoiced)
        {
            throw new BusinessRuleException("Faturalandırılmış siparişler iptal edilemez.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new BusinessRuleException("Sipariş zaten iptal edilmiş.");
        }

        db.Entry(order).Property(nameof(order.RowVersion)).OriginalValue = Convert.FromBase64String(r.RowVersion);

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Sipariş başka bir kullanıcı tarafından değiştirildi.");
        }

        return true;
    }
}
