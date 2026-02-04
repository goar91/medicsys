using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Models.Academico;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers.Academico;

[ApiController]
[Route("api/academic/appointments")]
[Authorize]
public class AcademicAppointmentsController : ControllerBase
{
    private readonly AcademicDbContext _db;

    public AcademicAppointmentsController(AcademicDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AcademicAppointmentDto>>> GetAll([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        var userId = GetUserId();
        var isProfessor = User.IsInRole(Roles.Professor);

        var query = _db.AcademicAppointments
            .Include(a => a.Student)
            .Include(a => a.Professor)
            .AsNoTracking();

        // Profesor ve todas las citas, Alumno solo las suyas
        if (!isProfessor)
        {
            query = query.Where(a => a.StudentId == userId);
        }

        if (start.HasValue)
            query = query.Where(a => a.StartAt >= start.Value);

        if (end.HasValue)
            query = query.Where(a => a.EndAt <= end.Value);

        var appointments = await query
            .OrderBy(a => a.StartAt)
            .ToListAsync();

        return Ok(appointments.Select(MapToDto));
    }

    [HttpPost]
    public async Task<ActionResult<AcademicAppointmentDto>> Create([FromBody] CreateAcademicAppointmentRequest request)
    {
        var userId = GetUserId();
        var isProfessor = User.IsInRole(Roles.Professor);

        // Solo profesores pueden crear citas
        if (!isProfessor)
            return Forbid();

        var student = await _db.Users.FindAsync(request.StudentId);
        var professor = await _db.Users.FindAsync(request.ProfessorId);

        if (student == null)
            return BadRequest(new { message = $"Estudiante con ID {request.StudentId} no encontrado." });

        if (professor == null)
            return BadRequest(new { message = $"Profesor con ID {request.ProfessorId} no encontrado." });

        var appointment = new AcademicAppointment
        {
            Id = Guid.NewGuid(),
            StudentId = request.StudentId,
            ProfessorId = request.ProfessorId,
            PatientName = request.PatientName,
            Reason = request.Reason,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Notes = request.Notes,
            Status = request.Status ?? AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.AcademicAppointments.Add(appointment);
        await _db.SaveChangesAsync();

        // Crear recordatorios automáticos
        await CreateRemindersAsync(appointment);

        appointment.Student = student;
        appointment.Professor = professor;

        return Ok(MapToDto(appointment));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AcademicAppointmentDto>> Update(Guid id, [FromBody] UpdateAcademicAppointmentRequest request)
    {
        var userId = GetUserId();
        var isProfessor = User.IsInRole(Roles.Professor);

        var appointment = await _db.AcademicAppointments
            .Include(a => a.Student)
            .Include(a => a.Professor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return NotFound();

        // Solo el profesor puede modificar
        if (!isProfessor)
            return Forbid();

        appointment.PatientName = request.PatientName;
        appointment.Reason = request.Reason;
        appointment.Notes = request.Notes;
        appointment.Status = request.Status ?? appointment.Status;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(MapToDto(appointment));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var isProfessor = User.IsInRole(Roles.Professor);

        if (!isProfessor)
            return Forbid();

        var appointment = await _db.AcademicAppointments.FindAsync(id);

        if (appointment == null)
            return NotFound();

        _db.AcademicAppointments.Remove(appointment);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task CreateRemindersAsync(AcademicAppointment appointment)
    {
        var student = await _db.Users.FindAsync(appointment.StudentId);
        var professor = await _db.Users.FindAsync(appointment.ProfessorId);

        if (student == null || professor == null)
            return;

        var reminders = new List<AcademicReminder>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                Message = $"Recordatorio: Cita con {student.FullName} - {appointment.Reason}",
                Channel = "Email",
                Status = "Pending",
                Target = professor.Email!,
                ScheduledAt = appointment.StartAt.AddHours(-24),
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                Message = $"Recordatorio: Tu cita de {appointment.Reason}",
                Channel = "Email",
                Status = "Pending",
                Target = student.Email!,
                ScheduledAt = appointment.StartAt.AddHours(-2),
                CreatedAt = DateTime.UtcNow
            }
        };

        _db.AcademicReminders.AddRange(reminders);
        await _db.SaveChangesAsync();
    }

    private static AcademicAppointmentDto MapToDto(AcademicAppointment a) => new(
        a.Id,
        a.StudentId,
        a.Student?.FullName ?? "",
        a.ProfessorId,
        a.Professor?.FullName ?? "",
        a.PatientName,
        a.Reason,
        a.StartAt,
        a.EndAt,
        a.Notes,
        a.Status.ToString(),
        a.CreatedAt
    );
}

public record CreateAcademicAppointmentRequest(
    Guid StudentId,
    Guid ProfessorId,
    string PatientName,
    string Reason,
    DateTime StartAt,
    DateTime EndAt,
    string? Notes,
    AppointmentStatus? Status
);

public record UpdateAcademicAppointmentRequest(
    string PatientName,
    string Reason,
    string? Notes,
    AppointmentStatus? Status
);

public record AcademicAppointmentDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid ProfessorId,
    string ProfessorName,
    string PatientName,
    string Reason,
    DateTime StartAt,
    DateTime EndAt,
    string? Notes,
    string Status,
    DateTime CreatedAt
);
