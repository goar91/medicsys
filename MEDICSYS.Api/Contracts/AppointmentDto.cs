namespace MEDICSYS.Api.Contracts;

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public Guid ProfessorId { get; set; }
    public string ProfessorName { get; set; } = string.Empty;
    public string ProfessorEmail { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? Notes { get; set; }
}
