using System.ComponentModel.DataAnnotations;

namespace MEDICSYS.Api.Contracts;

public class AppointmentRequest
{
    [Required]
    public Guid StudentId { get; set; }

    [Required]
    public Guid ProfessorId { get; set; }

    [Required]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }

    public string? Notes { get; set; }
}
