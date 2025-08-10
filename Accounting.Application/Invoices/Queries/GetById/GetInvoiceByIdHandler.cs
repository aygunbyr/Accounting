using Accounting.Application.Common.Abstractions;
using Accounting.Application.Invoices.Queries.Dto;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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

        var invC = CultureInfo.InvariantCulture;

        return new InvoiceDto(
            inv.Id,
            inv.ContactId,
            inv.DateUtc,
            inv.Currency,
            inv.TotalNet.ToString("F2", invC),
            inv.TotalVat.ToString("F2", invC),
            inv.TotalGross.ToString("F2", invC),
            inv.Lines.Select(l => new InvoiceLineDto(
                l.ItemId, l.Qty, l.UnitPrice, l.VatRate, l.Net, l.Vat, l.Gross
            )).ToList()
        );
    }
}
