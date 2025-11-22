using Accounting.Application.FixedAssets.Queries.Dto;
using MediatR;

namespace Accounting.Application.FixedAssets.Commands.Create;

public sealed record CreateFixedAssetCommand(
    string Code,
    string Name,
    DateTime PurchaseDateUtc,
    decimal PurchasePrice,
    int UsefulLifeYears
) : IRequest<FixedAssetDetailDto>;
