using Accounting.Application.Common.Models;
using Accounting.Application.Warehouses.Dto;
using MediatR;

namespace Accounting.Application.Warehouses.Queries.List;

public record ListWarehousesQuery(
    int BranchId,
    string? Search = null,           // code/name
    int PageNumber = 1,
    int PageSize = 20,
    string? Sort = "name:asc"        // code/name/isDefault
) : IRequest<PagedResult<WarehouseDto>>;
