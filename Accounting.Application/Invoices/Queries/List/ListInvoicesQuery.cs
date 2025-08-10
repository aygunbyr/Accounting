using Accounting.Application.Common.Models;
using Accounting.Application.Invoices.Queries.Dto;
using MediatR;

namespace Accounting.Application.Invoices.Queries.List;

public record ListInvoicesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Sort = "dateUtc:desc"
) : IRequest<PagedResult<InvoiceListItemDto>>;
