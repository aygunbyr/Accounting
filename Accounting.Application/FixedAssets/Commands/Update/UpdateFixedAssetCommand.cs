using Accounting.Application.FixedAssets.Queries.Dto;
using MediatR;

namespace Accounting.Application.FixedAssets.Commands.Update;

public sealed record UpdateFixedAssetCommand(
    int Id,
    string RowVersionBase64,
    string Code,
    string Name,
    DateTime PurchaseDateUtc,
    decimal PurchasePrice,
    int UsefulLifeYears
) : IRequest<FixedAssetDetailDto>;
