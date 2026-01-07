using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class SoftDeletePaymentHandler : IRequestHandler<SoftDeletePaymentCommand>
{
    private readonly IAppDbContext _db;
    private readonly IInvoiceBalanceService _balanceService;

    public SoftDeletePaymentHandler(IAppDbContext db, IInvoiceBalanceService balanceService)
    {
        _db = db;
        _balanceService = balanceService;
    }

    public async Task Handle(SoftDeletePaymentCommand req, CancellationToken ct)
    {
        var p = await _db.Payments.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (p is null) throw new NotFoundException("Payment", req.Id);

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

        // LinkedInvoiceId'yi sakla (SaveChanges sonrası kullanacağız)
        var linkedInvoiceId = p.LinkedInvoiceId;

        try
        {
            await _db.SaveChangesAsync(ct); // Önce soft delete'i kaydet
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("Ödeme başka biri tarafından güncellendi/silindi.");
        }

        // Sonra balance hesapla (artık Payment soft-deleted)
        if (linkedInvoiceId.HasValue)
        {
            await _balanceService.RecalculateBalanceAsync(linkedInvoiceId.Value, ct);
            await _db.SaveChangesAsync(ct);
        }
    }
}
