using System.ComponentModel.DataAnnotations;

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
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Supplier { get; set; } = string.Empty;

    [StringLength(60)]
    public string? InvoiceNumber { get; set; }

    [Required]
    public DateTime PurchaseDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [MinLength(1)]
    public List<CreatePurchaseItemRequest> Items { get; set; } = new();

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Received"; // Auto-recibir por defecto
}

public class CreatePurchaseItemRequest
{
    [Required]
    public Guid InventoryItemId { get; set; }

    [Range(1, 10_000)]
    public int Quantity { get; set; }

    [Range(0.01, 1_000_000)]
    public decimal UnitPrice { get; set; }
    public DateTime? ExpirationDate { get; set; }
}
