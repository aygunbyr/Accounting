using Accounting.Application.Common.Models;
using Accounting.Application.Invoices.Queries.Dto;
using MediatR;

namespace Accounting.Application.Invoices.Queries.List;

public enum InvoiceTypeFilter { Any = 0, Sales = 1, Purchase = 2 }

public record ListInvoicesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Sort = "dateUtc:desc",
    int? ContactId = null,
    InvoiceTypeFilter Type = InvoiceTypeFilter.Any,
    string? DateFromUtc = null, // ISO-8601 UTC
    string? DateToUtc = null // ISO-8601 UTC
) : IRequest<PagedResult<InvoiceListItemDto>>;
