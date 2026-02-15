using System.ComponentModel.DataAnnotations;

namespace MEDICSYS.Api.Models;

public class Invoice
{
    public Guid Id { get; set; }

    [MaxLength(32)]
    public string Number { get; set; } = string.Empty;

    [MaxLength(3)]
    public string EstablishmentCode { get; set; } = "001";

    [MaxLength(3)]
    public string EmissionPoint { get; set; } = "001";

    public int Sequential { get; set; }

    public DateTime IssuedAt { get; set; }

    [MaxLength(4)]
    public string CustomerIdentificationType { get; set; } = "05";

    [MaxLength(32)]
    public string CustomerIdentification { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CustomerAddress { get; set; }

    [MaxLength(32)]
    public string? CustomerPhone { get; set; }

    [MaxLength(120)]
    public string? CustomerEmail { get; set; }

    [MaxLength(300)]
    public string? Observations { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public decimal? CardFeePercent { get; set; }
    public decimal? CardFeeAmount { get; set; }
    public decimal TotalToCharge { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    [MaxLength(40)]
    public string? CardType { get; set; }

    public int? CardInstallments { get; set; }

    [MaxLength(120)]
    public string? PaymentReference { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

    [MaxLength(64)]
    public string? SriAccessKey { get; set; }

    [MaxLength(64)]
    public string? SriAuthorizationNumber { get; set; }

    public DateTime? SriAuthorizedAt { get; set; }

    public string? SriMessages { get; set; }

    [MaxLength(20)]
    public string SriEnvironment { get; set; } = "Pruebas";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<InvoiceItem> Items { get; set; } = new();
}
