using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEDICSYS.Api.Models.Odontologia;

public class Expense
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid OdontologoId { get; set; }

    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime ExpenseDate { get; set; }

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [StringLength(50)]
    public string PaymentMethod { get; set; } = "Efectivo"; // Efectivo, Tarjeta, Transferencia

    [StringLength(100)]
    public string? InvoiceNumber { get; set; }

    [StringLength(200)]
    public string? Supplier { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

public enum ExpenseCategory
{
    Supplies,          // Insumos
    Equipment,         // Equipamiento
    Maintenance,       // Mantenimiento
    Utilities,         // Servicios (luz, agua, internet)
    Rent,             // Alquiler
    Salaries,         // Salarios
    Marketing,        // Marketing
    Professional,     // Servicios profesionales
    Other             // Otros
}
