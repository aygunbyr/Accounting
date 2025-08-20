namespace Accounting.Application.Common.Models;

public record PagedResult<T>(
    int Total,
    int PageNumber,
    int PageSize,
    IReadOnlyList<T> Items,
    object? Totals = null // Payments'da PagedTotals, Invoices'da InvoicePagedTotals geçeceğiz
    );

// For payment
public record PagedTotals(
    string? PageTotalAmount,
    string? FilteredTotalAmount
    );

public record InvoicePagedTotals(
    string PageTotalNet,
    string PageTotalVat,
    string PageTotalGross,
    string FilteredTotalNet,
    string FilteredTotalVat,
    string FilteredTotalGross
    );

