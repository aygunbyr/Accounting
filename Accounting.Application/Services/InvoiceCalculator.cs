using Accounting.Domain.Entities;

namespace Accounting.Application.Services;

public static class InvoiceCalculator
{
    public static (decimal totalNet, decimal totalVat, decimal totalGross) Recalculate(Invoice invoice)
    {
        decimal tNet = 0, tVat = 0;

        foreach (var l in invoice.Lines)
        {
            var net = Math.Round(l.Qty * l.UnitPrice, 2, MidpointRounding.AwayFromZero);
            var vat = Math.Round(net * l.VatRate / 100m, 2, MidpointRounding.AwayFromZero);
            var gross = net + vat;

            l.Net = net; l.Vat = vat; l.Gross = gross;

            tNet += net; tVat += vat;
        }

        invoice.TotalNet = Math.Round(tNet, 2, MidpointRounding.AwayFromZero);
        invoice.TotalVat = Math.Round(tVat, 2, MidpointRounding.AwayFromZero);
        invoice.TotalGross = invoice.TotalNet + invoice.TotalVat;

        return (invoice.TotalNet, invoice.TotalVat, invoice.TotalGross);
    }
}
