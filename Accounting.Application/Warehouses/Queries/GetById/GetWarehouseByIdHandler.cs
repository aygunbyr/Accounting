using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Warehouses.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Warehouses.Queries.GetById;

public class GetWarehouseByIdHandler(IAppDbContext db)
    : IRequestHandler<GetWarehouseByIdQuery, WarehouseDto>
{
    public async Task<WarehouseDto> Handle(GetWarehouseByIdQuery r, CancellationToken ct)
    {
        var e = await db.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == r.Id && !x.IsDeleted, ct);

        if (e is null) throw new NotFoundException("Warehouse", r.Id);

        return new WarehouseDto(
            e.Id,
            e.BranchId,
            e.Code,
            e.Name,
            e.IsDefault,
            Convert.ToBase64String(e.RowVersion),
            e.CreatedAtUtc,
            e.UpdatedAtUtc
        );
    }
}
