using Accounting.Application.FixedAssets.Queries.Dto;
using MediatR;

namespace Accounting.Application.FixedAssets.Queries.GetById;

public sealed record GetFixedAssetByIdQuery(int Id)
    : IRequest<FixedAssetDetailDto>;
