using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Exceptions;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Orders.Commands.Approve;

public record ApproveOrderCommand(int Id, byte[] RowVersion) : IRequest<bool>;

public class ApproveOrderHandler(IAppDbContext db, IStockService stockService) : IRequestHandler<ApproveOrderCommand, bool>
{
    public async Task<bool> Handle(ApproveOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);

        if (order == null)
            throw new NotFoundException("Order", request.Id);

        // Optimistic Concurrency Check
        db.Entry(order).Property("RowVersion").OriginalValue = request.RowVersion;

        if (order.Status != OrderStatus.Draft)
            throw new BusinessRuleException("Sadece 'Taslak' durumundaki siparişler onaylanabilir.");

        // Validate Stock for Sales Orders
        if (order.Type == InvoiceType.Sales)
        {
            foreach (var line in order.Lines)
            {
                if (line.ItemId.HasValue)
                {
                    await stockService.ValidateStockAvailabilityAsync(line.ItemId.Value, line.Quantity, ct);
                }
            }
        }

        order.Status = OrderStatus.Approved;

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
