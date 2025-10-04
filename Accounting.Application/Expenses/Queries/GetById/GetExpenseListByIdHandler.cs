using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Utils;
using Accounting.Application.Expenses.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Expenses.Queries.GetById;

public class GetExpenseListByIdHandler
    : IRequestHandler<GetExpenseListByIdQuery, ExpenseListDetailDto>
{
    private readonly IAppDbContext _db;
    public GetExpenseListByIdHandler(IAppDbContext db) => _db = db;

    public async Task<ExpenseListDetailDto> Handle(GetExpenseListByIdQuery q, CancellationToken ct)
    {
        var list = await _db.ExpenseLists
            .Include(x => x.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == q.Id, ct);

        if (list is null)
            throw new KeyNotFoundException($"ExpenseList {q.Id} not found");

        // lines -> DTO
        var lineDtos = list.Lines
            .OrderBy(l => l.DateUtc)
            .Select(l => new ExpenseLineDto(
                l.Id,
                l.ExpenseListId,
                l.DateUtc,
                l.SupplierId,
                l.Currency,
                Money.S2(l.Amount), // string F2
                l.VatRate,
                l.Category,
                l.Notes
                ))
            .ToList();

        var total = list.Lines.Aggregate(0m, (acc, x) => acc + x.Amount);

        return new ExpenseListDetailDto(
            list.Id,
            list.Name,
            list.CreatedUtc,
            list.Status.ToString(),
            lineDtos,
            Money.S2(total),
            Convert.ToBase64String(list.RowVersion)
            );
    }
}
