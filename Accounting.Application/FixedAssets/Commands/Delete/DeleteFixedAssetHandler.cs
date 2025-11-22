using Accounting.Application.Common.Abstractions;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.FixedAssets.Commands.Delete;

public sealed class DeleteFixedAssetHandler
    : IRequestHandler<DeleteFixedAssetCommand>
{
    private readonly IAppDbContext _db;

    public DeleteFixedAssetHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DeleteFixedAssetCommand r, CancellationToken ct)
    {
        var entity = await _db.FixedAssets
            .FirstOrDefaultAsync(x => x.Id == r.Id, ct);

        if (entity is null)
        {
            throw new KeyNotFoundException("Fixed asset not found.");
        }

        // Concurrency: RowVersion
        if (string.IsNullOrWhiteSpace(r.RowVersionBase64))
        {
            throw new InvalidOperationException("RowVersion is required.");
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
