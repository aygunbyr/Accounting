namespace Accounting.Application.Reports.Queries;

public record DashboardStatsDto(
    string DailySalesTotal,
    string DailyCollectionsTotal,
    string TotalReceivables,
    string TotalPayables,
    List<CashStatusDto> CashStatus
);

public record CashStatusDto(
    int Id,
    string Name,
    string Type,
    string Balance,
    string Currency
);
