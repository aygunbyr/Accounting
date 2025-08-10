namespace Accounting.Domain.Entities;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = null!; // Intentionally uninitialized here; EF Core will set it (null-forgiving operator).
    public string Unit { get; set; } = "adet";
    public int VatRate { get; set; } = 20;
    public decimal? DefaultUnitPrice { get; set; }
}
