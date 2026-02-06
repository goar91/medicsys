using System.ComponentModel.DataAnnotations;
using MEDICSYS.Api.Models;

namespace MEDICSYS.Api.Contracts;

public class InvoiceItemRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 10_000)]
    public int Quantity { get; set; }

    [Range(0.01, 1_000_000)]
    public decimal UnitPrice { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }
}

public class InvoiceCreateRequest
{
    [Required]
    [StringLength(5, MinimumLength = 2)]
    public string CustomerIdentificationType { get; set; } = "05";

    [Required]
    [StringLength(20, MinimumLength = 6)]
    public string CustomerIdentification { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? CustomerAddress { get; set; }

    [StringLength(20)]
    public string? CustomerPhone { get; set; }

    [EmailAddress]
    [StringLength(120)]
    public string? CustomerEmail { get; set; }

    [StringLength(500)]
    public string? Observations { get; set; }

    [Required]
    [StringLength(20)]
    public string PaymentMethod { get; set; } = Models.PaymentMethod.Cash.ToString();

    [StringLength(50)]
    public string? CardType { get; set; }

    [Range(0, 100)]
    public decimal? CardFeePercent { get; set; }

    [Range(1, 48)]
    public int? CardInstallments { get; set; }

    [StringLength(120)]
    public string? PaymentReference { get; set; }

    public bool SendToSri { get; set; } = true;

    [MinLength(1)]
    public List<InvoiceItemRequest> Items { get; set; } = new();
}

public class InvoiceItemDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
}

public class InvoiceCustomerDto
{
    public string IdentificationType { get; set; } = string.Empty;
    public string Identification { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class InvoiceDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public int Sequential { get; set; }
    public DateTime IssuedAt { get; set; }

    public InvoiceCustomerDto Customer { get; set; } = new();

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public decimal? CardFeePercent { get; set; }
    public decimal? CardFeeAmount { get; set; }
    public decimal TotalToCharge { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;
    public string? CardType { get; set; }
    public int? CardInstallments { get; set; }
    public string? PaymentReference { get; set; }
    public string? Observations { get; set; }

    public string Status { get; set; } = string.Empty;
    public string? SriAccessKey { get; set; }
    public string? SriAuthorizationNumber { get; set; }
    public DateTime? SriAuthorizedAt { get; set; }
    public string? SriMessages { get; set; }

    public List<InvoiceItemDto> Items { get; set; } = new();
}
