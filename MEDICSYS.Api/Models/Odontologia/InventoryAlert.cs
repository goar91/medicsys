namespace MEDICSYS.Api.Models.Odontologia;

public enum AlertType
{
    LowStock,
    ExpirationWarning,
    Expired,
    OutOfStock
}

public class InventoryAlert
{
    public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public Guid OdontologoId { get; set; }
    public AlertType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public InventoryItem? InventoryItem { get; set; }
}
