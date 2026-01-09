using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Reports.Queries;

public record GetStockStatusQuery : IRequest<List<StockStatusDto>>;

public class GetStockStatusHandler(IAppDbContext db, IStockService stockService) : IRequestHandler<GetStockStatusQuery, List<StockStatusDto>>
{
    public async Task<List<StockStatusDto>> Handle(GetStockStatusQuery request, CancellationToken ct)
    {
        // 1. Fetch all active items
        var items = await db.Items
            .AsNoTracking()
            .Where(i => !i.IsDeleted)
            .Select(i => new { i.Id, i.Code, i.Name, i.Unit })
            .OrderBy(i => i.Name)
            .ToListAsync(ct);

        if (!items.Any()) return new List<StockStatusDto>();

        // 2. Calculate Stocks via Service
        var itemIds = items.Select(i => i.Id).ToList();
        var stockStats = await stockService.GetStockStatusAsync(itemIds, ct);
        var stockMap = stockStats.ToDictionary(k => k.ItemId);

        // 3. Map to DTO
        var result = new List<StockStatusDto>();

        foreach (var item in items)
        {
            var stats = stockMap.ContainsKey(item.Id) ? stockMap[item.Id] : new ItemStockDto(item.Id, 0, 0, 0, 0);

            result.Add(new StockStatusDto(
                item.Id,
                item.Code,
                item.Name,
                item.Unit,
                Money.S3(stats.QuantityIn),
                Money.S3(stats.QuantityOut),
                Money.S3(stats.QuantityReserved),
                Money.S3(stats.QuantityAvailable)
            ));
        }

        return result;
    }
}
