namespace MEDICSYS.Api.Contracts;

public class AppointmentReviewRequest
{
    public bool Approved { get; set; }
    public string? Notes { get; set; }
}
