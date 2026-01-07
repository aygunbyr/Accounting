using Accounting.Application.CashBankAccounts.Queries.Dto;
using Accounting.Application.Common.Errors;
using Accounting.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.CashBankAccounts.Queries.GetById;

public class GetCashBankAccountByIdHandler(IAppDbContext db)
    : IRequestHandler<GetCashBankAccountByIdQuery, CashBankAccountDetailDto>
{
    public async Task<CashBankAccountDetailDto> Handle(GetCashBankAccountByIdQuery r, CancellationToken ct)
    {
        var x = await db.CashBankAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == r.Id && !a.IsDeleted, ct);
        if (x is null) throw new NotFoundException("CashBankAccount", r.Id);

        return new CashBankAccountDetailDto(
            x.Id,
            x.Type.ToString(),
            x.Name,
            x.Iban,
            Convert.ToBase64String(x.RowVersion),
            x.CreatedAtUtc,
            x.UpdatedAtUtc
        );
    }
}
