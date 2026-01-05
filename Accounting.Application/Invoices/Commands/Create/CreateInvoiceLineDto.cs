namespace Accounting.Application.Invoices.Commands.Create;

public record CreateInvoiceLineDto(
    int? ItemId,
    int? ExpenseDefinitionId,
    string Qty,        // <-- string (3 hane)
    string UnitPrice,  // <-- string (4 hane)
    int VatRate
);
