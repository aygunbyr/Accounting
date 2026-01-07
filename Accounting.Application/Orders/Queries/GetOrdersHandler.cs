using Accounting.Application.Common.Abstractions;
using Accounting.Application.Orders.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Orders.Queries;

public record GetOrdersQuery() : IRequest<List<OrderDto>>;

public class GetOrdersHandler(IAppDbContext db) : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    public async Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .Include(o => o.Contact)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.DateUtc)
            .ToListAsync(cancellationToken);

        return orders.Select(o => new OrderDto(
            o.Id,
            null,
            o.OrderNumber,
            o.ContactId,
            o.Contact.Name,
            o.DateUtc,
            o.Status,
            o.TotalNet,
            o.TotalVat,
            o.TotalGross,
            o.Currency,
            o.Description,
            o.Lines.Select(l => new OrderLineDto(l.Id, l.ItemId, null, l.Description, l.Quantity, l.UnitPrice, l.VatRate, l.Total)).ToList(),
            o.CreatedAtUtc,
            Convert.ToBase64String(o.RowVersion)
        )).ToList();
    }
}
