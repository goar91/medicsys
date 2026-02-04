using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEDICSYS.Api.Models.Odontologia;

public class InventoryMovement
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OdontologoId { get; set; }

    [Required]
    public Guid InventoryItemId { get; set; }

    public InventoryItem? InventoryItem { get; set; }

    [Required]
    public DateTime MovementDate { get; set; }

    [Required]
    [StringLength(50)]
    public string MovementType { get; set; } = string.Empty; // Entry, Exit, Adjustment

    [Required]
    public int Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; }

    public int StockBefore { get; set; }

    public int StockAfter { get; set; }

    [StringLength(200)]
    public string? Reference { get; set; } // Purchase Order, Invoice, Adjustment Reason

    [StringLength(500)]
    public string? Notes { get; set; }

    public Guid? PurchaseOrderId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum InventoryMovementType
{
    Entry,      // Entrada (compra, devolución)
    Exit,       // Salida (venta, uso)
    Adjustment  // Ajuste (inventario físico)
}
