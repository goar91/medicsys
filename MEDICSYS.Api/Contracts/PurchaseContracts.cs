namespace MEDICSYS.Api.Contracts;

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string? Notes { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PurchaseItemDto> Items { get; set; } = new();
}

public class PurchaseItemDto
{
    public Guid Id { get; set; }
    public Guid InventoryItemId { get; set; }
    public string InventoryItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

public class CreatePurchaseOrderRequest
{
    public string Supplier { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string? Notes { get; set; }
    public List<CreatePurchaseItemRequest> Items { get; set; } = new();
    public string Status { get; set; } = "Received"; // Auto-recibir por defecto
}

public class CreatePurchaseItemRequest
{
    public Guid InventoryItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime? ExpirationDate { get; set; }
}
