using Accounting.Application.Common.Abstractions;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Reports.Queries;

public record GetProfitLossQuery(DateTime? DateFrom, DateTime? DateTo) : IRequest<ProfitLossDto>;

public class GetProfitLossHandler(IAppDbContext db) : IRequestHandler<GetProfitLossQuery, ProfitLossDto>
{
    public async Task<ProfitLossDto> Handle(GetProfitLossQuery request, CancellationToken ct)
    {
        // Date Filtering
        var dateFrom = request.DateFrom ?? DateTime.MinValue;
        var dateTo = request.DateTo ?? DateTime.MaxValue;

        // 1. Invoices (Income & COGS)
        var invoices = await db.Invoices
            .AsNoTracking()
            .Where(i => i.DateUtc >= dateFrom && i.DateUtc <= dateTo && !i.IsDeleted)
            .Select(i => new { i.Type, i.TotalNet, i.TotalVat })
            .ToListAsync(ct);

        var income = invoices.Where(i => i.Type == InvoiceType.Sales).Sum(i => i.TotalNet);
        var cogs = invoices.Where(i => i.Type == InvoiceType.Purchase).Sum(i => i.TotalNet); // Simplified: Buys are Costs
        
        var invoiceVat = invoices.Sum(i => i.Type == InvoiceType.Sales ? i.TotalVat : -i.TotalVat);

        // 2. Expenses
        var expenses = await db.ExpenseLines
            .AsNoTracking()
            .Where(e => e.DateUtc >= dateFrom && e.DateUtc <= dateTo && !e.IsDeleted)
            .Select(e => new { e.Amount, e.VatRate })
            .ToListAsync(ct);

        var totalExpenses = expenses.Sum(e => e.Amount);
        
        // Expense VAT calculation (Amount is Net, VAT is calculated from Rate)
        // Vat = Amount * Rate / 100
        var expenseVat = expenses.Sum(e => e.Amount * e.VatRate / 100m);

        // 3. Totals
        var grossProfit = income - cogs;
        var netProfit = grossProfit - totalExpenses;
        var totalVat = invoiceVat - expenseVat; // Net VAT Position (Payable/Receivable)

        return new ProfitLossDto(
            income,
            cogs,
            totalExpenses,
            grossProfit,
            netProfit,
            totalVat
        );
    }
}
