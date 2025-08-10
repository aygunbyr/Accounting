namespace Accounting.Domain.Entities
{
    public enum ContactType { Customer = 1, Vendor = 2 }
    public class Contact
    {
        public int Id { get; set; }
        public ContactType Type { get; set; } = ContactType.Customer;
        public string Name { get; set; } = null!;
        public string? TaxNo { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAtUtc { get; set; }
    }
}
