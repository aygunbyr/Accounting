using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Domain.Entities;
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

        // Payment kontrolü
        var hasPayments = await _db.Payments
            .AnyAsync(p => p.LinkedInvoiceId == req.Id && !p.IsDeleted, ct);

        if (hasPayments)
            throw new BusinessRuleException(
                "Cannot delete invoice with linked payments. Please delete payments first.");

        // ExpenseList kontrolü (PostToBill'den gelen)
        var hasExpenseLines = await _db.ExpenseLists
            .AnyAsync(e => e.PostedInvoiceId == req.Id && !e.IsDeleted, ct);

        if (hasExpenseLines)
            throw new BusinessRuleException(
                "Cannot delete invoice posted from expense list. Please delete expense list first.");

        // concurrency
        byte[] originalBytes;
        try
        {
            originalBytes = Convert.FromBase64String(req.RowVersion);
        }
        catch (FormatException)
        {
            throw new FluentValidation.ValidationException("RowVersion is not valid Base64.");
        }
        _db.Entry(inv).Property(nameof(Invoice.RowVersion)).OriginalValue = originalBytes;

        inv.IsDeleted = true;
        inv.DeletedAtUtc = DateTime.UtcNow;
        inv.UpdatedAtUtc = DateTime.UtcNow;

        // ---------------------------------------------------------
        // Stok hareketlerini de iptal et (Soft Delete)
        // ---------------------------------------------------------
        var refNote = $"Fatura Ref: {inv.Id}";
        var existingMovements = await _db.StockMovements
            .Where(m => m.Note == refNote && !m.IsDeleted)
            .ToListAsync(ct);

        foreach (var move in existingMovements)
        {
            move.IsDeleted = true;
            move.DeletedAtUtc = DateTime.UtcNow;
        }

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