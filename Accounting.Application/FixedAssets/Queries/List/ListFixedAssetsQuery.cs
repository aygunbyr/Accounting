using Accounting.Application.Common.Models;
using Accounting.Application.Common.Constants;
using Accounting.Application.FixedAssets.Queries.Dto;
using MediatR;

namespace Accounting.Application.FixedAssets.Queries.List;

public sealed record ListFixedAssetsQuery(
    int PageNumber = 1,
    int PageSize = PaginationConstants.DefaultPageSize,
    string? Search = null,
    bool IncludeDeleted = false
) : IRequest<PagedResult<FixedAssetListItemDto>>;
