using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Orders.Commands.Approve;

public record ApproveOrderCommand(int Id, string RowVersion) : IRequest<bool>;

public class ApproveOrderHandler(IAppDbContext db) : IRequestHandler<ApproveOrderCommand, bool>
{
    public async Task<bool> Handle(ApproveOrderCommand r, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == r.Id && !o.IsDeleted, ct);
        if (order is null) throw new NotFoundException("Order", r.Id);

        if (order.Status != OrderStatus.Draft)
        {
            throw new BusinessRuleException("Sadece taslak durumundaki siparişler onaylanabilir.");
        }

        db.Entry(order).Property(nameof(order.RowVersion)).OriginalValue = Convert.FromBase64String(r.RowVersion);

        order.Status = OrderStatus.Approved;
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
