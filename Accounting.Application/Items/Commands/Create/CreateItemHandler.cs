using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Utils;
using Accounting.Application.Items.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Items.Commands.Create;

public class CreateItemHandler : IRequestHandler<CreateItemCommand, ItemDetailDto>
{
    private readonly IAppDbContext _db;
    public CreateItemHandler(IAppDbContext db) => _db = db;

    public async Task<ItemDetailDto> Handle(CreateItemCommand r, CancellationToken ct)
    {
        decimal? price = null;
        if (r.DefaultUnitPrice is not null)
        {
            // Validator zaten kontrol ediyor; yine de güvenli parse:
            Money.TryParse2(r.DefaultUnitPrice, out var p);
            price = Money.R2(p);
        }

        var e = new Item
        {
            BranchId = r.BranchId,
            CategoryId = r.CategoryId,
            Name = r.Name.Trim(),
            Unit = r.Unit.Trim(),
            VatRate = r.VatRate,
            DefaultUnitPrice = price
            // Created/Updated, SoftDelete alanları audit interceptor tarafından set ediliyor
        };

        _db.Items.Add(e);
        await _db.SaveChangesAsync(ct);

        // Fresh read
        var saved = await _db.Items.AsNoTracking().FirstAsync(x => x.Id == e.Id, ct);

        // Kategori ismi
        string? catName = null;
        if (e.CategoryId.HasValue)
        {
             var cat = await _db.Categories.FindAsync(new object[] { e.CategoryId.Value }, ct);
             catName = cat?.Name;
        }

        return new ItemDetailDto(
            saved.Id,
            saved.CategoryId,
            catName,
            saved.Name,
            saved.Unit,
            saved.VatRate,
            saved.DefaultUnitPrice is null ? null : Money.S2(saved.DefaultUnitPrice.Value),
            Convert.ToBase64String(saved.RowVersion),
            saved.CreatedAtUtc,
            saved.UpdatedAtUtc
        );
    }
}
