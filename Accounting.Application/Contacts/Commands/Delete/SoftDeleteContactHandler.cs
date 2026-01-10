using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Exceptions;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Contacts.Commands.Delete;

public class SoftDeleteContactHandler : IRequestHandler<SoftDeleteContactCommand>
{
    private readonly IAppDbContext _db;
    public SoftDeleteContactHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SoftDeleteContactCommand req, CancellationToken ct)
    {
        var c = await _db.Contacts.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (c is null) throw new NotFoundException("Contact", req.Id);

        var original = Convert.FromBase64String(req.RowVersion);
        _db.Entry(c).Property(nameof(Contact.RowVersion)).OriginalValue = original;

        c.IsDeleted = true;
        c.DeletedAtUtc = DateTime.UtcNow;
        c.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Cari başka bir kullanıcı tarafından güncellendi/silindi.");
        }
    }
}
