using Accounting.Application.Common.Abstractions;
using Accounting.Application.Common.Utils; // Money.S2/S3/S4
using Accounting.Application.Invoices.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Application.Invoices.Queries.GetById;

public class GetInvoiceByIdHandler : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto>
{
    private readonly IAppDbContext _db;
    public GetInvoiceByIdHandler(IAppDbContext db) => _db = db;

    public async Task<InvoiceDto> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var inv = await _db.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (inv is null)
            throw new KeyNotFoundException($"Invoice {request.Id} not found.");

        var lines = inv.Lines
            .OrderBy(l => l.Id)
            .Select(l => new InvoiceLineDto(
                l.Id,
                l.ItemId,
                Money.S3(l.Qty),        // decimal -> "F3"
                Money.S4(l.UnitPrice),  // decimal -> "F4"
                l.VatRate,
                Money.S2(l.Net),        // decimal -> "F2"
                Money.S2(l.Vat),
                Money.S2(l.Gross)
            ))
            .ToList();

        return new InvoiceDto(
            inv.Id,
            inv.ContactId,
            inv.DateUtc,
            inv.Currency,
            Money.S2(inv.TotalNet),
            Money.S2(inv.TotalVat),
            Money.S2(inv.TotalGross),
            lines
        );
    }
}
