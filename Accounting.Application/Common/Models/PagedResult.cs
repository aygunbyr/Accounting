namespace Accounting.Application.Common.Models;

public record PagedResult<T>(
    int Total,
    int PageNumber,
    int PageSize,
    IReadOnlyList<T> Items,
    PagedTotals? Totals = null
    );

public record PagedTotals(
    string? PageTotalAmount,
    string? FilteredTotalAmount
    );