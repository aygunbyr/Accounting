using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Domain.Entities;
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

        byte[] originalBytes;
        try
        {
            originalBytes = Convert.FromBase64String(req.RowVersion);
        }
        catch (FormatException)
        {
            throw new FluentValidation.ValidationException("RowVersion is not valid Base64.");
        }

        _db.Entry(p).Property(nameof(Payment.RowVersion)).OriginalValue = originalBytes;

        p.IsDeleted = true;
        p.DeletedAtUtc = DateTime.UtcNow;
        p.UpdatedAtUtc = DateTime.UtcNow;

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
