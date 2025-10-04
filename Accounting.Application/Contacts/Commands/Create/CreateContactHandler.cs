using Accounting.Application.Common.Abstractions;
using Accounting.Application.Contacts.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;

namespace Accounting.Application.Contacts.Commands.Create;

public class CreateContactHandler : IRequestHandler<CreateContactCommand, ContactDto>
{
    private readonly IAppDbContext _db;
    public CreateContactHandler(IAppDbContext db) => _db = db;

    public async Task<ContactDto> Handle(CreateContactCommand req, CancellationToken ct)
    {
        var entity = new Contact
        {
            Name = req.Name.Trim(),
            Type = req.Type,
            Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim()
        };

        _db.Contacts.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new ContactDto(
            entity.Id,
            entity.Name,
            entity.Type.ToString(),
            entity.Email,
            Convert.ToBase64String(entity.RowVersion),
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc
            );
    }
}
