using Accounting.Application.Common.Utils;
using Accounting.Domain.Entities;

namespace Accounting.Application.Services;

public static class InvoiceCalculator
{
    public static (decimal totalNet, decimal totalVat, decimal totalGross) Recalculate(Invoice invoice)
    {
        decimal tNet = 0, tVat = 0;

        foreach (var l in invoice.Lines)
        {
            var net = Money.R2(l.Qty * l.UnitPrice);
            var vat = Money.R2(net * l.VatRate / 100m);
            var gross = net + vat;

            l.Net = net; l.Vat = vat; l.Gross = gross;

            tNet += net; tVat += vat;
        }

        invoice.TotalNet = Money.R2(tNet);
        invoice.TotalVat = Money.R2(tVat);
        invoice.TotalGross = invoice.TotalNet + invoice.TotalVat;

        return (invoice.TotalNet, invoice.TotalVat, invoice.TotalGross);
    }
}
