using Accounting.Application.CashBankAccounts.Queries.Dto;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Models;
using Accounting.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.CashBankAccounts.Queries.List;

public class ListCashBankAccountsHandler(IAppDbContext db)
    : IRequestHandler<ListCashBankAccountsQuery, PagedResult<CashBankAccountListItemDto>>
{
    public async Task<PagedResult<CashBankAccountListItemDto>> Handle(ListCashBankAccountsQuery q, CancellationToken ct)
    {
        var query = db.CashBankAccounts.AsNoTracking().Where(x => !x.IsDeleted);

        // Type filtresi: Any -> filtre yok; Cash/Bank -> eşitlik
        if (q.Type == CashBankAccountTypeFilter.Cash)
            query = query.Where(x => x.Type == CashBankAccountType.Cash);
        else if (q.Type == CashBankAccountTypeFilter.Bank)
            query = query.Where(x => x.Type == CashBankAccountType.Bank);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToUpperInvariant();
            query = query.Where(x => EF.Functions.Like(x.Name.ToUpper(), $"%{s}%"));
        }

        if (!string.IsNullOrWhiteSpace(q.IbanStartsWith))
        {
            var pfx = q.IbanStartsWith.Trim();
            query = query.Where(x => x.Iban != null && x.Iban.StartsWith(pfx));
        }

        query = (q.Sort?.ToLowerInvariant()) switch
        {
            "name:desc" => query.OrderByDescending(x => x.Name),
            "type:asc" => query.OrderBy(x => x.Type).ThenBy(x => x.Name),
            "type:desc" => query.OrderByDescending(x => x.Type).ThenBy(x => x.Name),
            _ => query.OrderBy(x => x.Name)
        };

        var total = await query.CountAsync(ct);

        var items = await query.Skip((q.PageNumber - 1) * q.PageSize)
                               .Take(q.PageSize)
                               .Select(x => new CashBankAccountListItemDto(
                                   x.Id,
                                   x.Type.ToString(),
                                   x.Name,
                                   x.Iban,
                                   x.CreatedAtUtc
                               ))
                               .ToListAsync(ct);

        return new PagedResult<CashBankAccountListItemDto>(total, q.PageNumber, q.PageSize, items, null);
    }
}
