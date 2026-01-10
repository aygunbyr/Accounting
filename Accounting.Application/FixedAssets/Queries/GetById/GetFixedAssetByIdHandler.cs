using System;
using Accounting.Application.Common.Exceptions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.FixedAssets.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.FixedAssets.Queries.GetById;

public sealed class GetFixedAssetByIdHandler
    : IRequestHandler<GetFixedAssetByIdQuery, FixedAssetDetailDto>
{
    private readonly IAppDbContext _db;

    public GetFixedAssetByIdHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<FixedAssetDetailDto> Handle(
        GetFixedAssetByIdQuery r,
        CancellationToken ct)
    {
        var x = await _db.FixedAssets
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == r.Id, ct);

        if (x is null)
        {
            throw new NotFoundException("FixedAsset", r.Id);
        }

        return new FixedAssetDetailDto(
            x.Id,
            x.Code,
            x.Name,
            x.PurchaseDateUtc,
            x.PurchasePrice,
            x.UsefulLifeYears,
            x.DepreciationRatePercent,
            x.IsDeleted,
            Convert.ToBase64String(x.RowVersion),
            x.CreatedAtUtc,
            x.UpdatedAtUtc,
            x.DeletedAtUtc
        );
    }
}
