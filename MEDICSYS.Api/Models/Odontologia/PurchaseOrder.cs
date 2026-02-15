using System.ComponentModel.DataAnnotations;

namespace MEDICSYS.Api.Models.Odontologia;

public class PurchaseOrder
{
    public Guid Id { get; set; }
    public Guid OdontologoId { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string? Notes { get; set; }
    public decimal Total { get; set; }
    public PurchaseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}

public class PurchaseItem
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid InventoryItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime? ExpirationDate { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }
    public InventoryItem? InventoryItem { get; set; }
}

public enum PurchaseStatus
{
    Pending,
    Received
}
