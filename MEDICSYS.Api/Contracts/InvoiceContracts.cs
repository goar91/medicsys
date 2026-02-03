using MEDICSYS.Api.Models;

namespace MEDICSYS.Api.Contracts;

public class InvoiceItemRequest
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
}

public class InvoiceCreateRequest
{
    public string CustomerIdentificationType { get; set; } = "05";
    public string CustomerIdentification { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerAddress { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Observations { get; set; }

    public string PaymentMethod { get; set; } = Models.PaymentMethod.Cash.ToString();
    public string? CardType { get; set; }
    public decimal? CardFeePercent { get; set; }
    public int? CardInstallments { get; set; }
    public string? PaymentReference { get; set; }

    public bool SendToSri { get; set; } = true;

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
