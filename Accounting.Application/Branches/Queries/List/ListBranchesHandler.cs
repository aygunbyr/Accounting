using Accounting.Application.Branches.Queries.Dto;
using Accounting.Application.Common.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Branches.Queries.List;

public sealed class ListBranchesHandler
    : IRequestHandler<ListBranchesQuery, IReadOnlyList<BranchDto>>
{
    private readonly IAppDbContext _ctx;

    public ListBranchesHandler(IAppDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<IReadOnlyList<BranchDto>> Handle(
        ListBranchesQuery request,
        CancellationToken ct)
    {
        // Varsayım: global query filter ile IsDeleted=false zaten uygulanıyor.
        // Yine de açıkça eklemek istersen:
        // .Where(b => !b.IsDeleted)

        var branches = await _ctx.Branches
            .AsNoTracking()
            .OrderBy(b => b.Code)
            .Select(x => new BranchDto(
                x.Id,
                x.Code,
                x.Name,
                Convert.ToBase64String(x.RowVersion ?? Array.Empty<byte>())
            ))
            .ToListAsync(ct);

        return branches;
    }
}
