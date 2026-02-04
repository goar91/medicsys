namespace MEDICSYS.Api.Models.Odontologia;

public class InventoryItem
{
    public Guid Id { get; set; }
    public Guid OdontologoId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public int Quantity { get; set; }
    public int MinimumQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Supplier { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsLowStock => Quantity <= MinimumQuantity;
    public bool IsExpiringSoon => ExpirationDate.HasValue && ExpirationDate.Value <= DateTime.UtcNow.AddMonths(1);
}
