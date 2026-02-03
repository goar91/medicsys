namespace MEDICSYS.Api.Models;

public class Appointment
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public ApplicationUser Student { get; set; } = null!;
    public Guid ProfessorId { get; set; }
    public ApplicationUser Professor { get; set; } = null!;
    public string PatientName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
