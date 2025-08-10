using Accounting.Application.Common.Abstractions;
using Accounting.Application.Services;
using Accounting.Domain.Entities;
using MediatR;
using System.Globalization;

namespace Accounting.Application.Invoices.Commands.Create;

public class CreateInvoiceHandler : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResult>
{
    private readonly IAppDbContext _db;

    public CreateInvoiceHandler(IAppDbContext db) => _db = db;
    
    public async Task<CreateInvoiceResult> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (!DateTime.TryParse(request.DateUtc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsed))
            throw new ArgumentException("DateUtc invalid format");

        var invoice = new Invoice
        {
            ContactId = request.ContactId,
            DateUtc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc),
            Currency = request.Currency,
            Direction = InvoiceDirection.Sale,
            Lines = request.Lines.Select(l => new InvoiceLine
            {
                ItemId = l.ItemId,
                Qty = l.Qty,
                UnitPrice = l.UnitPrice,
                VatRate = l.VatRate
            }).ToList()
        };

        InvoiceCalculator.Recalculate(invoice); // backend tek otorite (decimal)

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(cancellationToken);

        var inv = CultureInfo.InvariantCulture;
        return new CreateInvoiceResult(
            invoice.Id,
            invoice.TotalNet.ToString("F2", inv),
            invoice.TotalVat.ToString("F2", inv),
            invoice.TotalGross.ToString("F2", inv),
            "HALF_EVEN"
        );

    }
}
