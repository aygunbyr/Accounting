using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Extensions; // ApplyBranchFilter
using Accounting.Application.Common.Interfaces; // ICurrentUserService
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.FixedAssets.Commands.Delete;

public sealed class DeleteFixedAssetHandler
    : IRequestHandler<DeleteFixedAssetCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public DeleteFixedAssetHandler(IAppDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteFixedAssetCommand r, CancellationToken ct)
    {
        var entity = await _db.FixedAssets
            .ApplyBranchFilter(_currentUserService)
            .FirstOrDefaultAsync(x => x.Id == r.Id, ct);

        if (entity is null)
        {
            throw new NotFoundException("FixedAsset", r.Id);
        }

        // Concurrency: RowVersion
        if (string.IsNullOrWhiteSpace(r.RowVersionBase64))
        {
            throw new BusinessRuleException("RowVersion is required.");
        }

        var originalRowVersion = Convert.FromBase64String(r.RowVersionBase64);
        _db.Entry(entity).Property(nameof(FixedAsset.RowVersion)).OriginalValue = originalRowVersion;

        if (!entity.IsDeleted)
        {
            entity.IsDeleted = true;
            entity.DeletedAtUtc = DateTime.UtcNow;
            entity.UpdatedAtUtc = entity.DeletedAtUtc;
        }

        await _db.SaveChangesAsync(ct);
    }
}
