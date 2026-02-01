using System.ComponentModel.DataAnnotations;

namespace MEDICSYS.Api.Contracts;

public class ClinicalHistoryReviewRequest
{
    [Required]
    public bool Approved { get; set; }

    public string? Notes { get; set; }
}
