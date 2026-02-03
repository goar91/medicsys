using System.ComponentModel.DataAnnotations;

namespace MEDICSYS.Api.Models;

public class AccountingEntry
{
    public Guid Id { get; set; }

    public DateTime Date { get; set; }

    public AccountingEntryType Type { get; set; }

    public Guid CategoryId { get; set; }
    public AccountingCategory Category { get; set; } = null!;

    [MaxLength(220)]
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    [MaxLength(120)]
    public string? Reference { get; set; }

    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    [MaxLength(32)]
    public string Source { get; set; } = "Manual";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
