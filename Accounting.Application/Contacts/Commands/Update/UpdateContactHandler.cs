using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors; // ConcurrencyConflictException
using Accounting.Application.Contacts.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Contacts.Commands.Update;

public class UpdateContactHandler : IRequestHandler<UpdateContactCommand, ContactDto>
{
    private readonly IAppDbContext _db;
    public UpdateContactHandler(IAppDbContext db) => _db = db;

    public async Task<ContactDto> Handle(UpdateContactCommand req, CancellationToken ct)
    {
        // 1) Fetch (TRACKING)
        var c = await _db.Contacts.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (c is null) throw new NotFoundException("Contact", req.Id);

        // 2) Business rules: (şimdilik yok)

        // 3) Concurrency
        byte[] rv;
        try { rv = Convert.FromBase64String(req.RowVersion); }
        catch { throw new ConcurrencyConflictException("RowVersion geçersiz."); }
        _db.Entry(c).Property(nameof(Contact.RowVersion)).OriginalValue = rv;

        // 4) Normalize / map
        c.Name = req.Name.Trim();
        c.Type = req.Type;
        c.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();

        // 5) Audit
        c.UpdatedAtUtc = DateTime.UtcNow;

        // 6) Persist
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        { throw new ConcurrencyConflictException("Cari başka biri tarafından güncellendi."); }

        // 7) Fresh read
        var fresh = await _db.Contacts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (fresh is null) throw new NotFoundException("Contact", req.Id);

        // 8) DTO
        return new ContactDto(
            fresh.Id,
            fresh.BranchId,
            fresh.Code,
            fresh.Name,
            fresh.Type.ToString(),
            fresh.Email,
            Convert.ToBase64String(fresh.RowVersion),
            fresh.CreatedAtUtc,
            fresh.UpdatedAtUtc
        );
    }
}
