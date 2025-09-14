using Accounting.Application.Common.Abstractions;
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

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Fatura başka biri tarafından güncellendi/silindi.");
        }
    }
}
