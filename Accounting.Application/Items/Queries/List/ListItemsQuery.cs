using Accounting.Application.Common.Models;
using Accounting.Application.Items.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;

namespace Accounting.Application.Items.Queries.List;

public record ListItemsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,         // Name contains (case-insensitive)
    string? Unit = null,           // eşleşirse filtre
    int? VatRate = null,           // 0..100
    string? Sort = "name:asc"      // "name:asc|desc", "vatRate:asc|desc", "price:asc|desc"
) : IRequest<PagedResult<ItemListItemDto>>;
