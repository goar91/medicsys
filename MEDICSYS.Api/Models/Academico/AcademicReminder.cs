namespace MEDICSYS.Api.Models.Academico;

public class AcademicReminder
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string Message { get; set; } = null!;
    public string Channel { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Target { get; set; } = null!;
    public DateTime ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public AcademicAppointment Appointment { get; set; } = null!;
}
