// Accounting.Application/Items/Commands/Update/UpdateItemHandler.cs
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;   // ConcurrencyConflictException
using Accounting.Application.Common.Utils;
using Accounting.Application.Items.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Items.Commands.Update;

public class UpdateItemHandler : IRequestHandler<UpdateItemCommand, ItemDetailDto>
{
    private readonly IAppDbContext _db;
    public UpdateItemHandler(IAppDbContext db) => _db = db;

    public async Task<ItemDetailDto> Handle(UpdateItemCommand r, CancellationToken ct)
    {
        // (1) fetch (tracking)
        var e = await _db.Items.FirstOrDefaultAsync(i => i.Id == r.Id && !i.IsDeleted, ct);
        if (e is null) throw new KeyNotFoundException("Item not found.");

        // (3) original rowversion
        var original = Convert.FromBase64String(r.RowVersion);
        _db.Entry(e).Property("RowVersion").OriginalValue = original;

        // (4) normalize + parse
        e.Name = r.Name.Trim();
        e.Unit = r.Unit.Trim();
        e.VatRate = r.VatRate;

        if (r.DefaultUnitPrice is null)
        {
            e.DefaultUnitPrice = null;
        }
        else
        {
            Money.TryParse2(r.DefaultUnitPrice, out var p);
            e.DefaultUnitPrice = Money.R2(p);
        }

        // (6) save + concurrency
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Item güncellemesinde eşzamanlılık hatası.");
        }

        // (7) fresh read
        var fresh = await _db.Items.AsNoTracking().FirstAsync(x => x.Id == e.Id, ct);

        // (8) dto
        return new ItemDetailDto(
            fresh.Id,
            fresh.Name,
            fresh.Unit,
            fresh.VatRate,
            fresh.DefaultUnitPrice is null ? null : Money.S2(fresh.DefaultUnitPrice.Value),
            Convert.ToBase64String(fresh.RowVersion),
            fresh.CreatedAtUtc,
            fresh.UpdatedAtUtc
        );
    }
}
