using Accounting.Application.Common.Abstractions;
using Accounting.Application.Expenses.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Expenses.Commands.Review;

public class ReviewExpenseListHandler : IRequestHandler<ReviewExpenseListCommand, ExpenseListDto>
{
    private readonly IAppDbContext _db;
    public ReviewExpenseListHandler(IAppDbContext db) => _db = db;

    public async Task<ExpenseListDto> Handle(ReviewExpenseListCommand req, CancellationToken ct)
    {
        var list = await _db.ExpenseLists
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);

        if (list is null)
            throw new KeyNotFoundException($"ExpenseList {req.Id} not found.");

        if (list.Status != ExpenseListStatus.Draft)
            throw new InvalidOperationException("Only Draft lists can be reviewed.");

        if (!list.Lines.Any())
            throw new InvalidOperationException("Expense list must have at least one line to review.");

        list.Status = ExpenseListStatus.Reviewed;
        await _db.SaveChangesAsync(ct);

        return new ExpenseListDto(list.Id, list.Name, list.CreatedAtUtc, list.Status.ToString());

    }
}
