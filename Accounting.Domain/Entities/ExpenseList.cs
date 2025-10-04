namespace Accounting.Domain.Entities;

public enum ExpenseListStatus
{
    Draft = 1,
    Reviewed = 2,
    Posted = 3, // fatura oluşturuldu
}

public class ExpenseList
{
    public int Id { get; set; }
    public string Name { get; set; } = "Masraf Listesi";
    public DateTime CreatedUtc { get; set; }
    public ExpenseListStatus Status { get; set; } = ExpenseListStatus.Draft;

    // Post edildiğinde oluşturulan fatura Id'si
    public int? PostedInvoiceId { get; set; }

    public ICollection<Expense> Lines { get; set; } = new List<Expense>();

    public bool IsDeleted { get; set; } // soft delete
    public byte[] RowVersion { get; set; } = null!; // optimistic concurrency

}
