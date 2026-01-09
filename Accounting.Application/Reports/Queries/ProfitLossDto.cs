namespace Accounting.Application.Reports.Queries;

public record ProfitLossDto(
    decimal Income,
    decimal CostOfGoods,
    decimal Expenses,
    decimal GrossProfit,
    decimal NetProfit,
    decimal TotalVat
);
