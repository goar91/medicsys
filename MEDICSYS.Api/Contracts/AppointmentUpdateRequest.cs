using System.ComponentModel.DataAnnotations;
using MEDICSYS.Api.Models;

namespace MEDICSYS.Api.Contracts;

public class AppointmentUpdateRequest
{
    [StringLength(120, MinimumLength = 2)]
    public string? PatientName { get; set; }

    [StringLength(200, MinimumLength = 3)]
    public string? Reason { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
    public AppointmentStatus? Status { get; set; }
}
