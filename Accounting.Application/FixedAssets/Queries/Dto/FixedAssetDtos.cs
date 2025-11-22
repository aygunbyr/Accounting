namespace Accounting.Application.FixedAssets.Queries.Dto;

public sealed record FixedAssetListItemDto(
    int Id,
    string Code,
    string Name,
    DateTime PurchaseDateUtc,
    decimal PurchasePrice,
    int UsefulLifeYears,
    decimal DepreciationRatePercent
);

public sealed record FixedAssetDetailDto(
    int Id,
    string Code,
    string Name,
    DateTime PurchaseDateUtc,
    decimal PurchasePrice,
    int UsefulLifeYears,
    decimal DepreciationRatePercent,
    bool IsDeleted,
    string RowVersionBase64,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? DeletedAtUtc
);
