using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Application.Common.Utils;
using Accounting.Application.Orders.Dto;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Orders.Commands.Update;

public record UpdateOrderCommand(
    int Id,
    int ContactId,
    DateTime DateUtc,
    string? Description,
    List<UpdateOrderLineDto> Lines,
    string RowVersion
) : IRequest<OrderDto>;

public record UpdateOrderLineDto(
    int? Id, // Null = New Line
    int? ItemId,
    string Description,
    string Quantity,
    string UnitPrice,
    int VatRate
);

public class UpdateOrderHandler(IAppDbContext db) : IRequestHandler<UpdateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(UpdateOrderCommand r, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == r.Id && !o.IsDeleted, ct);

        if (order is null) throw new NotFoundException("Order", r.Id);

        if (order.Status != OrderStatus.Draft)
        {
            throw new BusinessRuleException("Sadece taslak durumundaki siparişler güncellenebilir.");
        }

        db.Entry(order).Property(nameof(order.RowVersion)).OriginalValue = Convert.FromBase64String(r.RowVersion);

        // Update Header
        order.ContactId = r.ContactId;
        order.DateUtc = r.DateUtc;
        order.Description = r.Description;
        order.UpdatedAtUtc = DateTime.UtcNow;

        // Update Lines
        // 1. Delete removed lines
        var reqLineIds = r.Lines.Where(l => l.Id.HasValue).Select(l => l.Id!.Value).ToList();
        var toRemove = order.Lines.Where(l => !reqLineIds.Contains(l.Id)).ToList();
        foreach (var rm in toRemove)
        {
            db.OrderLines.Remove(rm);
        }

        // 2. Add/Update lines
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

            if (l.Id.HasValue)
            {
                var existing = order.Lines.First(x => x.Id == l.Id.Value);
                existing.ItemId = l.ItemId;
                existing.Description = l.Description;
                existing.Quantity = qty;
                existing.UnitPrice = price;
                existing.VatRate = l.VatRate;
                existing.Total = lineNet;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                order.Lines.Add(new OrderLine
                {
                    ItemId = l.ItemId,
                    Description = l.Description,
                    Quantity = qty,
                    UnitPrice = price,
                    VatRate = l.VatRate,
                    Total = lineNet
                });
            }
        }

        order.TotalNet = totalNet;
        order.TotalVat = totalVat;
        order.TotalGross = totalNet + totalVat;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Sipariş başka bir kullanıcı tarafından değiştirildi.");
        }

        return new OrderDto(
            order.Id,
            null,
            order.OrderNumber,
            order.ContactId,
            "",
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
