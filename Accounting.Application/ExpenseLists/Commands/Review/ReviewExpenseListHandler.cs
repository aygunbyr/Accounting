using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Application.ExpenseLists.Dto;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.ExpenseLists.Commands.Review;

public class ReviewExpenseListHandler : IRequestHandler<ReviewExpenseListCommand, ExpenseListDto>
{
    private readonly IAppDbContext _db;
    public ReviewExpenseListHandler(IAppDbContext db) => _db = db;

    public async Task<ExpenseListDto> Handle(ReviewExpenseListCommand req, CancellationToken ct)
    {
        var list = await _db.ExpenseLists
            .Include(x => x.Lines.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);

        if (list is null)
            throw new KeyNotFoundException($"ExpenseList {req.Id} not found.");

        if (list.Status != ExpenseListStatus.Draft)
            throw new BusinessRuleException("Only Draft expense lists can be reviewed.");

        if (!list.Lines.Any())
            throw new BusinessRuleException("Expense list must have at least one line to review.");

        list.Status = ExpenseListStatus.Reviewed;
        list.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new ExpenseListDto(
            Id: list.Id,
            BranchId: list.BranchId,
            Name: list.Name,
            Status: list.Status.ToString(),
            CreatedAtUtc: list.CreatedAtUtc
        );
    }
}