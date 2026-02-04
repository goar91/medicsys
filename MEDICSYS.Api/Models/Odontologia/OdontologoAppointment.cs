namespace MEDICSYS.Api.Models.Odontologia;

public class OdontologoAppointment
{
    public Guid Id { get; set; }
    public Guid OdontologoId { get; set; }
    public string PatientName { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Notes { get; set; }
    public AppointmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ApplicationUser Odontologo { get; set; } = null!;
}
