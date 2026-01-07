using System.Globalization;
using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Errors;  // ConcurrencyConflictException
using Accounting.Application.Common.Utils;   // Money.*
using Accounting.Application.Invoices.Queries.Dto;
using Accounting.Domain.Entities;
using Accounting.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Invoices.Commands.UpdateHeader;

public class UpdateInvoiceHeaderHandler
    : IRequestHandler<UpdateInvoiceHeaderCommand, InvoiceDto>
{
    private readonly IAppDbContext _db;
    public UpdateInvoiceHeaderHandler(IAppDbContext db) => _db = db;

    public async Task<InvoiceDto> Handle(UpdateInvoiceHeaderCommand req, CancellationToken ct)
    {
        // 1) Fetch (TRACKING)
        var inv = await _db.Invoices
            .Include(i => i.Lines) // header toplamları ve DTO için satırlar gerekli
            .FirstOrDefaultAsync(i => i.Id == req.Id, ct);

        if (inv is null)
            throw new NotFoundException("Invoice", req.Id);

        // 2) Concurrency (parent RowVersion)
        byte[] rv;
        try { rv = Convert.FromBase64String(req.RowVersion); }
        catch { throw new ConcurrencyConflictException("RowVersion geçersiz."); }
        _db.Entry(inv).Property(nameof(Invoice.RowVersion)).OriginalValue = rv;

        // 3) Normalize/map
        inv.ContactId = req.ContactId;
        inv.Type = req.Type;

        if (req.Type != inv.Type)
        {
             // Fatura türü değişirse (Sales -> SalesReturn) eski satırlar pozitif kalır ama mantık değişir mi?
             // "All Positive" kuralında Sales ve SalesReturn verileri farksızdır (hepsi pozitif).
             // Sadece header Type farklıdır.
             // Dolayısıyla Type değiştirmek artık DAHA GÜVENLİ ama yine de accounting açısından riskli.
             // Biz yine de değiştirmeye izin vermeyelim.
             throw new BusinessRuleException("Fatura türü değiştirilemez. Lütfen faturayı silip yeniden oluşturun.");
        }

        // 4) Audit
        inv.UpdatedAtUtc = DateTime.UtcNow;

        // 5) Header toplamlarını Type'a göre yeniden işaretle (satırlar değişmedi)
        // Satırlar zaten positive. Toplamlar da positive.
        inv.TotalNet = Money.R2(inv.Lines.Sum(x => x.Net));
        inv.TotalVat = Money.R2(inv.Lines.Sum(x => x.Vat));
        inv.TotalGross = Money.R2(inv.Lines.Sum(x => x.Gross));

        // 6) Persist
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        { throw new ConcurrencyConflictException("Fatura başka biri tarafından güncellendi."); }

        // 7) Fresh read (Contact + Lines) — Snapshot alanlarını kullanacağımız için Item Include'a gerek yok
        var fresh = await _db.Invoices
            .AsNoTracking()
            .Include(i => i.Contact)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == req.Id, ct);

        if (fresh is null)
            throw new NotFoundException("Invoice", req.Id);

        // 8) Lines → DTO (SNAPSHOT KULLAN)
        var lines = fresh.Lines
            .OrderBy(l => l.Id)
            .Select(l => new InvoiceLineDto(
                l.Id,
                l.ItemId,
                l.ExpenseDefinitionId, // Added
                l.ItemCode,     // snapshot
                l.ItemName,     // snapshot
                l.Unit,         // snapshot
                Money.S3(l.Qty),
                Money.S4(l.UnitPrice),
                l.VatRate,
                Money.S2(l.Net),
                Money.S2(l.Vat),
                Money.S2(l.Gross)
            ))
            .ToList();

        return new InvoiceDto(
            fresh.Id,
            fresh.ContactId,
            fresh.Contact.Code,
            fresh.Contact.Name,
            fresh.DateUtc,
            fresh.Currency,
            Money.S2(fresh.TotalNet),
            Money.S2(fresh.TotalVat),
            Money.S2(fresh.TotalGross),
            Money.S2(fresh.Balance),
            lines,
            Convert.ToBase64String(fresh.RowVersion),
            fresh.CreatedAtUtc,
            fresh.UpdatedAtUtc,
            (int)fresh.Type,
            fresh.BranchId,
            fresh.Branch.Code,
            fresh.Branch.Name
        );
    }
}
