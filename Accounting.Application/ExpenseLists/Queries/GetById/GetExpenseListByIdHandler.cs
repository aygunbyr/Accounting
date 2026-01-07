using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Application.Common.Utils;
using Accounting.Application.ExpenseLists.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.ExpenseLists.Queries.GetById;

public class GetExpenseListByIdHandler : IRequestHandler<GetExpenseListByIdQuery, ExpenseListDetailDto>
{
    private readonly IAppDbContext _db;
    public GetExpenseListByIdHandler(IAppDbContext db) => _db = db;

    public async Task<ExpenseListDetailDto> Handle(GetExpenseListByIdQuery q, CancellationToken ct)
    {
        var list = await _db.ExpenseLists
            .Include(x => x.Lines.Where(l => !l.IsDeleted))
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == q.Id && !x.IsDeleted, ct);

        if (list is null)
            throw new NotFoundException("ExpenseList", q.Id);

        var lineDtos = list.Lines
            .OrderBy(l => l.DateUtc)
            .Select(l => new ExpenseLineDto(
                Id: l.Id,
                ExpenseListId: l.ExpenseListId,
                DateUtc: l.DateUtc,
                SupplierId: l.SupplierId,
                Currency: l.Currency,
                Amount: Money.S2(l.Amount),
                VatRate: l.VatRate,
                Category: l.Category,
                Notes: l.Notes
            ))
            .ToList();

        var total = list.Lines.Sum(l => l.Amount);

        return new ExpenseListDetailDto(
            Id: list.Id,
            BranchId: list.BranchId,
            Name: list.Name,
            Status: list.Status.ToString(),
            Lines: lineDtos,
            TotalAmount: Money.S2(total),
            CreatedAtUtc: list.CreatedAtUtc,
            UpdatedAtUtc: list.UpdatedAtUtc,
            RowVersion: Convert.ToBase64String(list.RowVersion)
        );
    }
}