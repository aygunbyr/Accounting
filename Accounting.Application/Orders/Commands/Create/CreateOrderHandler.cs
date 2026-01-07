using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Utils;
using Accounting.Application.Orders.Dto;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Orders.Commands.Create;

public record CreateOrderCommand(
    int BranchId,
    int ContactId,
    DateTime DateUtc,
    InvoiceType Type,
    string Currency,
    string? Description,
    List<CreateOrderLineDto> Lines
) : IRequest<OrderDto>;

public record CreateOrderLineDto(
    int? ItemId,
    string Description,
    string Quantity,
    string UnitPrice,
    int VatRate
);

public class CreateOrderHandler(IAppDbContext db) : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand r, CancellationToken ct)
    {
        // 1. Generate Order Number (Simple max+1 logic for MVP)
        // Usually should be DB sequence or specialized service
        var lastOrder = await db.Orders
            .Where(o => o.BranchId == r.BranchId && o.Type == r.Type)
            .OrderByDescending(o => o.OrderNumber)
            .FirstOrDefaultAsync(ct);

        long nextNum = 1;
        if (lastOrder != null && long.TryParse(lastOrder.OrderNumber, out var lastN))
        {
            nextNum = lastN + 1;
        }
        var orderNumber = nextNum.ToString().PadLeft(6, '0');

        // 2. Create Order
        var order = new Order
        {
            BranchId = r.BranchId,
            ContactId = r.ContactId,
            OrderNumber = orderNumber,
            DateUtc = r.DateUtc,
            Type = r.Type,
            Status = OrderStatus.Draft,
            Currency = r.Currency ?? "TRY",
            Description = r.Description,
            CreatedAtUtc = DateTime.UtcNow,
            RowVersion = []
        };

        decimal totalNet = 0;
        decimal totalVat = 0;

        foreach (var l in r.Lines)
        {
            Money.TryParse3(l.Quantity, out var qty);
            Money.TryParse2(l.UnitPrice, out var price);

            var lineNet = Money.R2(qty * price);
            var vatAmount = Money.R2(lineNet * l.VatRate / 100m);

            totalNet += lineNet;
            totalVat += vatAmount;

            order.Lines.Add(new OrderLine
            {
                ItemId = l.ItemId,
                Description = l.Description,
                Quantity = qty,
                UnitPrice = price,
                VatRate = l.VatRate,
                Total = lineNet // Storing Net Total line-by-line
            });
        }

        order.TotalNet = totalNet;
        order.TotalVat = totalVat;
        order.TotalGross = totalNet + totalVat;

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        // Return DTO
        return new OrderDto(
            order.Id,
            order.BranchId,
            order.OrderNumber,
            order.ContactId,
            "", // Contact name - could be fetched if needed
            order.DateUtc,
            order.Status,
            order.TotalNet,
            order.TotalVat,
            order.TotalGross,
            order.Currency,
            order.Description,
            order.Lines.Select(x => new OrderLineDto(x.Id, x.ItemId, null, x.Description, x.Quantity, x.UnitPrice, x.VatRate, x.Total)).ToList(),
            order.CreatedAtUtc,
            Convert.ToBase64String(order.RowVersion)
        );
    }
}
