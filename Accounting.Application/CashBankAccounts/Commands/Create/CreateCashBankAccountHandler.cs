using Accounting.Application.CashBankAccounts.Queries.Dto;
using Accounting.Application.Common.Abstractions;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.CashBankAccounts.Commands.Create;

public class CreateCashBankAccountHandler(IAppDbContext db)
    : IRequestHandler<CreateCashBankAccountCommand, CashBankAccountDetailDto>
{
    public async Task<CashBankAccountDetailDto> Handle(CreateCashBankAccountCommand r, CancellationToken ct)
    {
        var e = new CashBankAccount
        {
            Type = r.Type,                               // <-- doğrudan enum
            Name = r.Name.Trim(),
            Iban = string.IsNullOrWhiteSpace(r.Iban) ? null : r.Iban.Trim()
        };

        db.CashBankAccounts.Add(e);
        await db.SaveChangesAsync(ct);

        var fresh = await db.CashBankAccounts.AsNoTracking().FirstAsync(x => x.Id == e.Id, ct);

        return new CashBankAccountDetailDto(
            fresh.Id,
            fresh.Type.ToString(),
            fresh.Name,
            fresh.Iban,
            Convert.ToBase64String(fresh.RowVersion),
            fresh.CreatedAtUtc,
            fresh.UpdatedAtUtc
        );
    }
}
