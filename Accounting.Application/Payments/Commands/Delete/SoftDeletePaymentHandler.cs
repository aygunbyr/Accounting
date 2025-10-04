using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class SoftDeletePaymentHandler : IRequestHandler<SoftDeletePaymentCommand>
{
    private readonly IAppDbContext _db;
    public SoftDeletePaymentHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SoftDeletePaymentCommand req, CancellationToken ct)
    {
        var p = await _db.Payments.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (p is null) throw new KeyNotFoundException($"Payment {req.Id} not found.");

        var original = Convert.FromBase64String(req.RowVersion);
        _db.Entry(p).Property("RowVersion").OriginalValue = original;

        p.IsDeleted = true;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Ödeme başka biri tarafından güncellendi/silindi.");
        }
    }
}
