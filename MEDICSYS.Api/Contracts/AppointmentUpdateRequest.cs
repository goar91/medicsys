using MEDICSYS.Api.Models;

namespace MEDICSYS.Api.Contracts;

public class AppointmentUpdateRequest
{
    public string? PatientName { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public AppointmentStatus? Status { get; set; }
}
