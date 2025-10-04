using Accounting.Application.Common.Abstractions;
using Accounting.Application.Contacts.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Contacts.Queries.GetById;

public class GetContactByIdHandler : IRequestHandler<GetContactByIdQuery, ContactDto>
{
    private readonly IAppDbContext _db;
    public GetContactByIdHandler(IAppDbContext db) => _db = db;

    public async Task<ContactDto> Handle(GetContactByIdQuery q, CancellationToken ct)
    {
        var c = await _db.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == q.Id, ct);

        if (c is null)
            throw new KeyNotFoundException($"Contact {q.Id} not found");

        return new ContactDto(
            c.Id, c.Name, c.Type.ToString(), c.Email,
            Convert.ToBase64String(c.RowVersion),
            c.CreatedAtUtc, c.UpdatedAtUtc
            );
    }
}
