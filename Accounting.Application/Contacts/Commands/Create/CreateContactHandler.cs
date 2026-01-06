using Accounting.Application.Common.Abstractions;
using Accounting.Application.Contacts.Queries.Dto;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Contacts.Commands.Create;

public class CreateContactHandler : IRequestHandler<CreateContactCommand, ContactDto>
{
    private readonly IAppDbContext _db;
    public CreateContactHandler(IAppDbContext db) => _db = db;

    public async Task<ContactDto> Handle(CreateContactCommand req, CancellationToken ct)
    {
        // Auto-generate Code: CRI-{BranchId}-{Sequence}
        var code = await GenerateCodeAsync(req.BranchId, ct);

        var entity = new Contact
        {
            BranchId = req.BranchId,
            Code = code,
            Name = req.Name.Trim(),
            Type = req.Type,
            Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim()
        };

        _db.Contacts.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new ContactDto(
            entity.Id,
            entity.BranchId,
            entity.Code,
            entity.Name,
            entity.Type.ToString(),
            entity.Email,
            Convert.ToBase64String(entity.RowVersion),
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc
            );
    }

    private async Task<string> GenerateCodeAsync(int branchId, CancellationToken ct)
    {
        // Şubeye ait en son Contact kodunu bul
        var lastCode = await _db.Contacts
            .IgnoreQueryFilters() // Soft-deleted olanları da say
            .Where(c => c.BranchId == branchId)
            .OrderByDescending(c => c.Id)
            .Select(c => c.Code)
            .FirstOrDefaultAsync(ct);

        int nextSequence = 1;

        if (!string.IsNullOrEmpty(lastCode))
        {
            // Format: CRI-{BranchId}-{Sequence}
            var parts = lastCode.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastSeq))
            {
                nextSequence = lastSeq + 1;
            }
        }

        return $"CRI-{branchId}-{nextSequence:D5}";
    }
}
