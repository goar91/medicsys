using System.ComponentModel.DataAnnotations;
using MEDICSYS.Api.Models;

namespace MEDICSYS.Api.Contracts;

public class AppointmentRequest
{
    public Guid? StudentId { get; set; }

    public Guid? ProfessorId { get; set; }

    [Required(ErrorMessage = "El nombre del paciente es requerido")]
    [MinLength(1, ErrorMessage = "El nombre del paciente no puede estar vacío")]
    public string PatientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La razón de la cita es requerida")]
    public string Reason { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha y hora de inicio es requerida")]
    public DateTime StartAt { get; set; }

    [Required(ErrorMessage = "La fecha y hora de fin es requerida")]
    public DateTime EndAt { get; set; }

    public AppointmentStatus? Status { get; set; }

    public string? Notes { get; set; }
}
