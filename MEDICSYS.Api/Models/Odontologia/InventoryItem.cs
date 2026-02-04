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
    public int? MaximumQuantity { get; set; }        // Kardex: Stock máximo
    public int? ReorderPoint { get; set; }           // Kardex: Punto de reorden
    public decimal UnitPrice { get; set; }
    public decimal? AverageCost { get; set; }        // Kardex: Costo promedio
    public string? Supplier { get; set; }
    public string? Location { get; set; }            // Kardex: Ubicación física
    public string? Batch { get; set; }               // Kardex: Lote
    public DateTime? ExpirationDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed properties
    public bool IsLowStock => Quantity <= MinimumQuantity;
    public bool IsExpiringSoon => ExpirationDate.HasValue && ExpirationDate.Value <= DateTime.UtcNow.AddMonths(1);
    public bool NeedsReorder => ReorderPoint.HasValue && Quantity <= ReorderPoint.Value;
}
