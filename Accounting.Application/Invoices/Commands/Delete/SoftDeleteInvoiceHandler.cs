using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Invoices.Commands.Delete;

public class SoftDeleteInvoiceHandler : IRequestHandler<SoftDeleteInvoiceCommand>
{
    private readonly IAppDbContext _db;
    public SoftDeleteInvoiceHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SoftDeleteInvoiceCommand req, CancellationToken ct)
    {
        var inv = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == req.Id, ct);

        if (inv is null)
            throw new KeyNotFoundException($"Invoice {req.Id} not found.");

        // concurrency
        var originalBytes = Convert.FromBase64String(req.RowVersion);
        _db.Entry(inv).Property("RowVersion").OriginalValue = originalBytes;

        inv.IsDeleted = true;
        inv.DeletedAtUtc = DateTime.UtcNow;
        inv.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
        "Kayıt başka bir kullanıcı tarafından güncellendi. Lütfen sayfayı yenileyip tekrar deneyin.");
        }
    }
}
