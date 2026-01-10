using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.ExpenseDefinitions.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.ExpenseDefinitions.Queries.GetById;

public sealed class GetExpenseDefinitionByIdHandler
    : IRequestHandler<GetExpenseDefinitionByIdQuery, ExpenseDefinitionDetailDto>
{
    private readonly IAppDbContext _db;

    public GetExpenseDefinitionByIdHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<ExpenseDefinitionDetailDto> Handle(
        GetExpenseDefinitionByIdQuery r,
        CancellationToken ct)
    {
        var x = await _db.ExpenseDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == r.Id, ct);

        if (x is null)
        {
            throw new NotFoundException("ExpenseDefinition", r.Id);
        }

        return new ExpenseDefinitionDetailDto(
            x.Id,
            x.Code,
            x.Name,
            x.DefaultVatRate,
            x.IsActive,
            Convert.ToBase64String(x.RowVersion),
            x.CreatedAtUtc,
            x.UpdatedAtUtc
        );
    }
}
