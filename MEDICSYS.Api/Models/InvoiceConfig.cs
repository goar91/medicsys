using System.ComponentModel.DataAnnotations;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Models;

/// <summary>
/// Single-row configuration for invoice numbering.
/// Stores establishment code, emission point.
/// The sequential is derived from existing invoices.
/// </summary>
public class InvoiceConfig
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(3)]
    [RegularExpression(@"^\d{1,3}$")]
    public string EstablishmentCode { get; set; } = "001";

    [Required]
    [MaxLength(3)]
    [RegularExpression(@"^\d{1,3}$")]
    public string EmissionPoint { get; set; } = "002";

    public DateTime UpdatedAt { get; set; } = DateTimeHelper.Now();
}
