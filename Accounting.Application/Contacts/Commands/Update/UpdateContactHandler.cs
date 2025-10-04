using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors; // ConcurrencyConflictException
using Accounting.Application.Contacts.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Contacts.Commands.Update;

public class UpdateContactHandler : IRequestHandler<UpdateContactCommand, ContactDto>
{
    private readonly IAppDbContext _db;
    public UpdateContactHandler(IAppDbContext db) => _db = db;

    public async Task<ContactDto> Handle(UpdateContactCommand req, CancellationToken ct)
    {
        var c = await _db.Contacts.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (c is null) throw new KeyNotFoundException($"Contact {req.Id} not found.");

        // concurrency
        var original = Convert.FromBase64String(req.RowVersion);
        _db.Entry(c).Property("RowVersion").OriginalValue = original;

        c.Name = req.Name.Trim();
        c.Type = req.Type;
        c.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "Cari başka bir kullanıcı tarafından güncellendi. Lütfen sayfayı yenileyip tekrar deneyin.");
        }

        return new ContactDto(
            c.Id, c.Name, c.Type.ToString(), c.Email,
            Convert.ToBase64String(c.RowVersion),
            c.CreatedAtUtc, c.UpdatedAtUtc
        );
    }
}
