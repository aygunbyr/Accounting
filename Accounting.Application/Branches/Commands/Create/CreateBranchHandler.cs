using Accounting.Application.Branches.Queries.Dto;
using Accounting.Application.Common.Abstractions;
using Accounting.Domain.Entities;
using MediatR;

namespace Accounting.Application.Branches.Commands.Create;

public record CreateBranchCommand(string Code, string Name) : IRequest<BranchDto>;

public class CreateBranchHandler : IRequestHandler<CreateBranchCommand, BranchDto>
{
    private readonly IAppDbContext _context;

    public CreateBranchHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<BranchDto> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        var entity = new Branch
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Branches.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new BranchDto(
            entity.Id, 
            entity.Code, 
            entity.Name, 
            Convert.ToBase64String(entity.RowVersion ?? Array.Empty<byte>())
        );
    }
}
