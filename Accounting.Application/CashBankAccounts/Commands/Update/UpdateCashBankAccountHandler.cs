using Accounting.Application.CashBankAccounts.Queries.Dto;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.CashBankAccounts.Commands.Update;

public class UpdateCashBankAccountHandler(IAppDbContext db)
    : IRequestHandler<UpdateCashBankAccountCommand, CashBankAccountDetailDto>
{
    public async Task<CashBankAccountDetailDto> Handle(UpdateCashBankAccountCommand r, CancellationToken ct)
    {
        var e = await db.CashBankAccounts.FirstOrDefaultAsync(a => a.Id == r.Id && !a.IsDeleted, ct);
        if (e is null) throw new NotFoundException("CashBankAccount", r.Id);

        db.Entry(e).Property(nameof(CashBankAccount.RowVersion)).OriginalValue = Convert.FromBase64String(r.RowVersion);

        e.Type = r.Type;                                 // <-- doğrudan enum
        e.Name = r.Name.Trim();
        e.Iban = string.IsNullOrWhiteSpace(r.Iban) ? null : r.Iban.Trim();

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Hesap başka biri tarafından güncellendi.");
        }

        var fresh = await db.CashBankAccounts.AsNoTracking().FirstAsync(x => x.Id == r.Id, ct);

        return new CashBankAccountDetailDto(
            fresh.Id,
            fresh.BranchId,
            fresh.Code,
            fresh.Type.ToString(),
            fresh.Name,
            fresh.Iban,
            Convert.ToBase64String(fresh.RowVersion),
            fresh.CreatedAtUtc,
            fresh.UpdatedAtUtc
        );
    }
}
