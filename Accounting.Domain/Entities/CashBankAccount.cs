namespace Accounting.Domain.Entities
{
    public enum CashBankAccountType { Cash = 1, Bank = 2 }
    public class CashBankAccount
    {
        public int Id { get; set; }
        public CashBankAccountType Type { get; set; } = CashBankAccountType.Cash;
        public string Name { get; set; } = null!; // Intentionally uninitialized here; EF Core will set it (null-forgiving operator).
        public string? Iban { get; set; }
    }
}
