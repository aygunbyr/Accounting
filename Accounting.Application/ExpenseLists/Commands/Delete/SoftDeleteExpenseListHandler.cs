using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.ExpenseLists.Commands.Delete;

public class SoftDeleteExpenseListHandler : IRequestHandler<SoftDeleteExpenseListCommand>
{
    private readonly IAppDbContext _db;
    public SoftDeleteExpenseListHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SoftDeleteExpenseListCommand req, CancellationToken ct)
    {
        var list = await _db.ExpenseLists
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);

        if (list is null)
            throw new KeyNotFoundException($"ExpenseList {req.Id} not found.");

        // Concurrency check
        byte[] originalBytes;
        try
        {
            originalBytes = Convert.FromBase64String(req.RowVersion);
        }
        catch (FormatException)
        {
            throw new FluentValidation.ValidationException("RowVersion is not valid Base64.");
        }
        _db.Entry(list).Property(nameof(ExpenseList.RowVersion)).OriginalValue = originalBytes;

        // Business rules
        if (list.Status == ExpenseListStatus.Posted)
            throw new BusinessRuleException("Posted expense lists cannot be deleted.");

        if (list.Status == ExpenseListStatus.Reviewed)
            throw new BusinessRuleException("Reviewed expense lists cannot be deleted.");

        // Soft delete parent
        var now = DateTime.UtcNow;
        list.IsDeleted = true;
        list.DeletedAtUtc = now;
        list.UpdatedAtUtc = now;

        // Soft delete children
        foreach (var line in list.Lines)
        {
            line.IsDeleted = true;
            line.DeletedAtUtc = now;
            line.UpdatedAtUtc = now;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "Kayıt başka bir kullanıcı tarafından güncellendi. Lütfen sayfayı yenileyip tekrar deneyin.");
        }
    }
}