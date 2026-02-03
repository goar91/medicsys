using System.ComponentModel.DataAnnotations;

namespace MEDICSYS.Api.Models;

public class InvoiceItem
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxRate { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }
}
