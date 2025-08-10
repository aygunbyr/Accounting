﻿namespace Accounting.Domain.Entities;

public class InvoiceLine
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int ItemId { get; set; }

    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public int VatRate { get; set; }

    public decimal Net { get; set; }
    public decimal Vat { get; set; }
    public decimal Gross { get; set; }
}
