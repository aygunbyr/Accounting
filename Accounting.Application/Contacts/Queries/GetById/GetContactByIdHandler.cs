using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Extensions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Contacts.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Contacts.Queries.GetById;

public class GetContactByIdHandler : IRequestHandler<GetContactByIdQuery, ContactDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    
    public GetContactByIdHandler(IAppDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<ContactDto> Handle(GetContactByIdQuery q, CancellationToken ct)
    {
        var c = await _db.Contacts
            .AsNoTracking()
            .ApplyBranchFilter(_currentUserService)
            .FirstOrDefaultAsync(x => x.Id == q.Id, ct);

        if (c is null)
            throw new NotFoundException("Contact", q.Id);

        return new ContactDto(
            c.Id, c.BranchId, c.Code, c.Name, c.Type.ToString(), c.Email,
            Convert.ToBase64String(c.RowVersion),
            c.CreatedAtUtc, c.UpdatedAtUtc
            );
    }
}
