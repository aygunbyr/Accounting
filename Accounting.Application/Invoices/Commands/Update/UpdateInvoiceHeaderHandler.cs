using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;
using Accounting.Application.Common.Utils;
using Accounting.Application.Invoices.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Accounting.Application.Invoices.Commands.Update;

public class UpdateInvoiceHeaderHandler
    : IRequestHandler<UpdateInvoiceHeaderCommand, InvoiceDto>
{
    private readonly IAppDbContext _db;
    public UpdateInvoiceHeaderHandler(IAppDbContext db) => _db = db;

    public async Task<InvoiceDto> Handle(UpdateInvoiceHeaderCommand req, CancellationToken ct)
    {
        var inv = await _db.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == req.Id, ct);

        if (inv is null)
            throw new KeyNotFoundException($"Invoice {req.Id} not found.");

        // optimistic concurrency: RowVersion karşılaştır
        var originalBytes = Convert.FromBase64String(req.RowVersion);
        _db.Entry(inv).Property("RowVersion").OriginalValue = originalBytes;

        // güncelle
        inv.ContactId = req.ContactId;
        inv.Currency = req.Currency.ToUpperInvariant();
        inv.Type = req.Type;

        if (!DateTime.TryParse(req.DateUtc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dt))
            throw new ArgumentException("DateUtc is invalid.");
        inv.DateUtc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        // toplamlara dokunmuyoruz (header update)

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
         "Kayıt başka bir kullanıcı tarafından güncellendi. Lütfen sayfayı yenileyip tekrar deneyin.");
        }

        // dto
        var lines = inv.Lines.OrderBy(l => l.Id).Select(l => new InvoiceLineDto(
            l.Id, l.ItemId,
            Money.S3(l.Qty),
            Money.S4(l.UnitPrice),
            l.VatRate,
            Money.S2(l.Net),
            Money.S2(l.Vat),
            Money.S2(l.Gross)
        )).ToList();

        return new InvoiceDto(
            inv.Id,
            inv.ContactId,
            inv.DateUtc,
            inv.Currency,
            Money.S2(inv.TotalNet),
            Money.S2(inv.TotalVat),
            Money.S2(inv.TotalGross),
            lines,
            Convert.ToBase64String(inv.RowVersion)
        );
    }
}
