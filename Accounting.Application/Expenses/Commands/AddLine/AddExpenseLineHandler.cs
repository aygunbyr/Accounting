using System.Globalization;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Utils;
using Accounting.Application.Expenses.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;

namespace Accounting.Application.Expenses.Commands.AddLine;

public class AddExpenseLineHandler : IRequestHandler<AddExpenseLineCommand, ExpenseLineDto>
{
    private readonly IAppDbContext _db;
    public AddExpenseLineHandler(IAppDbContext db) => _db = db;

    public async Task<ExpenseLineDto> Handle(AddExpenseLineCommand req, CancellationToken ct)
    {
        // Parse date utc
        if (!DateTime.TryParse(req.DateUtc, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal, out var dateUtc))
            throw new ArgumentException("DateUtc invalid");

        // Parse amount (string -> decimal)
        if (!decimal.TryParse(req.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            throw new ArgumentException("Amount invalid");

        amount = Money.R2(amount); // AwayFromZero

        // Liste var mı?
        var list = await _db.ExpenseLists.FindAsync(new object?[] { req.ExpenseListId }, ct);
        if (list is null)
            throw new KeyNotFoundException($"ExpenseList {req.ExpenseListId} not found.");

        if (list.Status != ExpenseListStatus.Draft && list.Status != ExpenseListStatus.Reviewed)
            throw new InvalidOperationException("Only Draft/Reviewed lists can accept new lines.");

        var line = new Expense
        {
            ExpenseListId = req.ExpenseListId,
            DateUtc = DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc),
            SupplierId = req.SupplierId,
            Currency = req.Currency,
            Amount = amount,
            VatRate = req.VatRate,
            Category = string.IsNullOrWhiteSpace(req.Category) ? null : req.Category!.Trim(),
            Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes!.Trim()
        };

        _db.Expenses.Add(line);
        await _db.SaveChangesAsync(ct);

        return new ExpenseLineDto(
            line.Id,
            line.ExpenseListId,
            line.DateUtc,
            line.SupplierId,
            line.Currency,
            Money.S2(line.Amount), // string F2
            line.VatRate,
            line.Category,
            line.Notes
        );
    }
}
