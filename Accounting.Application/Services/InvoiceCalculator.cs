using Accounting.Domain.Entities;

namespace Accounting.Application.Services;

public class InvoiceCalculator
{
    public static (decimal totalNet, decimal totalVat, decimal totalGross) Recalculate(Invoice invoice)
    {
        decimal tNet = 0, tVat = 0;

        foreach(var line in invoice.Lines)
        {
            var net = Math.Round(line.Qty * line.UnitPrice, 2, MidpointRounding.ToEven);
            var vat = Math.Round(net * line.VatRate / 100m, 2, MidpointRounding.ToEven);
            var gross = net + vat;

            line.Net = net;
            line.Vat = vat;
            line.Gross = gross;

            tNet += net; 
            tVat += vat;
        }

        invoice.TotalNet = Math.Round(tNet, 2, MidpointRounding.ToEven);
        invoice.TotalVat = Math.Round(tVat, 2, MidpointRounding.ToEven);
        invoice.TotalGross = invoice.TotalNet + invoice.TotalVat;

        return (invoice.TotalNet, invoice.TotalVat, invoice.TotalGross);
    }
}
