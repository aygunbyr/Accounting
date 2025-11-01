using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.CashBankAccounts.Commands.Delete;

public class SoftDeleteCashBankAccountHandler(IAppDbContext db)
    : IRequestHandler<SoftDeleteCashBankAccountCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteCashBankAccountCommand r, CancellationToken ct)
    {
        var e = await db.CashBankAccounts.FirstOrDefaultAsync(a => a.Id == r.Id && !a.IsDeleted, ct);
        if (e is null) throw new KeyNotFoundException($"CashBankAccount {r.Id} not found.");

        db.Entry(e).Property(nameof(CashBankAccount.RowVersion)).OriginalValue = Convert.FromBase64String(r.RowVersion);

        e.IsDeleted = true;
        e.DeletedAtUtc = DateTime.UtcNow;   // Contacts desenine paralel
        e.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Hesap silinirken eşzamanlılık hatası.");
        }
    }
}
