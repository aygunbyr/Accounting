using Accounting.Application.Common.Abstractions;
using Accounting.Application.Expenses.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;

namespace Accounting.Application.Expenses.Commands.CreateList;

public class CreateExpenseListHandler : IRequestHandler<CreateExpenseListCommand, ExpenseListDto>
{
    private readonly IAppDbContext _db;
    public CreateExpenseListHandler(IAppDbContext db) => _db = db;

    public async Task<ExpenseListDto> Handle(CreateExpenseListCommand req, CancellationToken ct)
    {
        var entity = new ExpenseList
        {
            Name = string.IsNullOrWhiteSpace(req.Name) ? "Masraf Listesi" : req.Name.Trim()
        };

        _db.ExpenseLists.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new ExpenseListDto(
            entity.Id,
            entity.Name,
            entity.CreatedAtUtc,
            entity.Status.ToString()
        );
    }
}
