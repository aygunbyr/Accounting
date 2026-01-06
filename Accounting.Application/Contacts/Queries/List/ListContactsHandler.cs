using Accounting.Application.Common.Abstractions;
using Accounting.Application.Contacts.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Contacts.Queries.List;

public class ListContactsHandler : IRequestHandler<ListContactsQuery, ContactListResult>
{
    private readonly IAppDbContext _db;
    public ListContactsHandler(IAppDbContext db) => _db = db;

    public async Task<ContactListResult> Handle(ListContactsQuery q, CancellationToken ct)
    {
        var qry = _db.Contacts.AsNoTracking();

        // BranchId filter (eğer belirtilmişse)
        if (q.BranchId.HasValue)
        {
            qry = qry.Where(x => x.BranchId == q.BranchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            qry = qry.Where(x => x.Name.Contains(s) || x.Code.Contains(s) || (x.Email != null && x.Email.Contains(s)));
        }
        if (q.Type.HasValue)
        {
            qry = qry.Where(x => x.Type == q.Type.Value);
        }

        var total = await qry.CountAsync(ct);

        var items = await qry
            .OrderBy(x => x.Name)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(x => new ContactListItemDto(
                x.Id, x.BranchId, x.Code, x.Name, x.Type.ToString(), x.Email,
                x.CreatedAtUtc
            ))
            .ToListAsync(ct);

        return new ContactListResult(total, items);
    }
}
