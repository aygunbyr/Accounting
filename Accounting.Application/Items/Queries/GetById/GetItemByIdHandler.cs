using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Utils;
using Accounting.Application.Items.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Items.Queries.GetById;

public class GetItemByIdHandler(IAppDbContext db) : IRequestHandler<GetItemByIdQuery, ItemDetailDto>
{
    public async Task<ItemDetailDto> Handle(GetItemByIdQuery r, CancellationToken ct)
    {
        var x = await db.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == r.Id && !i.IsDeleted, ct);
        if (x is null) throw new NotFoundException("Item", r.Id);

        return new ItemDetailDto(
            x.Id, 
            x.CategoryId, 
            x.Category?.Name,
            x.Name, x.Unit, x.VatRate,
            x.PurchasePrice is null ? null : Money.S2(x.PurchasePrice.Value),
            x.SalesPrice is null ? null : Money.S2(x.SalesPrice.Value),
            Convert.ToBase64String(x.RowVersion),
            x.CreatedAtUtc, x.UpdatedAtUtc
        );
    }
}
