using System.ComponentModel.DataAnnotations;

namespace MEDICSYS.Api.Contracts;

public class AppointmentReviewRequest
{
    [Required]
    public bool Approved { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
