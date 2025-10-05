using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Utils;
using Accounting.Application.Expenses.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Expenses.Queries.GetLineById;

public class GetExpenseLineByIdHandler
    : IRequestHandler<GetExpenseLineByIdQuery, ExpenseLineDto>
{
    private readonly IAppDbContext _db;
    public GetExpenseLineByIdHandler(IAppDbContext db) => _db = db;

    public async Task<ExpenseLineDto> Handle(GetExpenseLineByIdQuery q, CancellationToken ct)
    {
        var line = await _db.Expenses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == q.Id, ct);

        if (line is null)
            throw new KeyNotFoundException($"Expense line {q.Id} not found.");

        return new ExpenseLineDto(
            line.Id,
            line.ExpenseListId,
            line.DateUtc,
            line.SupplierId,
            line.Currency,
            Money.S2(line.Amount),
            line.VatRate,
            line.Category,
            line.Notes
            );
    }
}
