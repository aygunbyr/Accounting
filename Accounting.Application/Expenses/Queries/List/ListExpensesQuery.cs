using Accounting.Application.Common.Models;
using Accounting.Application.Expenses.Queries.Dto;
using MediatR;

namespace Accounting.Application.Expenses.Queries.List;

public record ListExpensesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Sort = "dateUtc:desc", // "dateUtc|amount:asc|desc"
    int? ExpenseListId = null,
    int? SupplierId = null,
    string? Currency = null, // "TRY"
    string? Category = null,
    string? DateFromUtc = null, // ISO-8601 UTC
    string? DateToUtc = null // ISO-8601 UTC
) : IRequest<PagedResult<ExpenseLineDto>>;