using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Application.Orders.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Orders.Queries.GetById;

public class GetOrderByIdHandler(IAppDbContext db) : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderByIdQuery r, CancellationToken ct)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Lines.Where(l => !l.IsDeleted))
                .ThenInclude(l => l.Item)
            .Include(o => o.Contact)
            .FirstOrDefaultAsync(o => o.Id == r.Id && !o.IsDeleted, ct);

        if (order is null)
            throw new NotFoundException("Order", r.Id);

        return new OrderDto(
            order.Id,
            order.BranchId,
            order.OrderNumber,
            order.ContactId,
            order.Contact.Name,
            order.DateUtc,
            order.Status,
            order.TotalNet,
            order.TotalVat,
            order.TotalGross,
            order.Currency,
            order.Description,
            order.Lines.Select(l => new OrderLineDto(
                l.Id,
                l.ItemId,
                l.Item?.Name,
                l.Description,
                l.Quantity,
                l.UnitPrice,
                l.VatRate,
                l.Total
            )).ToList(),
            order.CreatedAtUtc,
            Convert.ToBase64String(order.RowVersion)
        );
    }
}
