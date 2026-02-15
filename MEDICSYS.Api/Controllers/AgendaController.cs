using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Contracts;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers;

[ApiController]
[Route("api/agenda")]
public class AgendaController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AgendaController> _logger;

    public AgendaController(AppDbContext db, UserManager<ApplicationUser> userManager, ILogger<AgendaController> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    [Authorize]
    [HttpGet("appointments")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointments(
        [FromQuery] Guid? studentId,
        [FromQuery] Guid? professorId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        var userId = GetUserId();
        var isProvider = User.IsInRole(Roles.Professor) || User.IsInRole(Roles.Odontologo);

        var query = _db.Appointments
            .Include(x => x.Student)
            .Include(x => x.Professor)
            .AsNoTracking();

        if (isProvider)
        {
            professorId ??= userId;
            query = query.Where(x => x.ProfessorId == professorId.Value);
            if (studentId.HasValue)
            {
                query = query.Where(x => x.StudentId == studentId.Value);
            }
        }
        else
        {
            query = query.Where(x => x.StudentId == userId);
        }

        var total = await query.CountAsync();

        if (page.HasValue || pageSize.HasValue)
        {
            var pageValue = Math.Max(1, page ?? 1);
            var sizeValue = Math.Clamp(pageSize ?? 50, 1, 200);
            query = query
                .OrderBy(x => x.StartAt)
                .Skip((pageValue - 1) * sizeValue)
                .Take(sizeValue);

            Response.Headers["X-Total-Count"] = total.ToString();
            Response.Headers["X-Page"] = pageValue.ToString();
            Response.Headers["X-Page-Size"] = sizeValue.ToString();
        }
        else
        {
            query = query.OrderBy(x => x.StartAt);
        }

        var items = await query.ToListAsync();
        return Ok(items.Select(Map));
    }

    [Authorize]
    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment(AppointmentRequest request)
    {
        _logger.LogInformation("CreateAppointment called with payload: {@Request}", request);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model validation failed: {@Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return BadRequest(ModelState);
        }

        if (request.StartAt >= request.EndAt)
        {
            return BadRequest(new { message = "La hora de inicio debe ser menor a la hora de fin." });
        }

        var userId = GetUserId();
        var isProfessor = User.IsInRole(Roles.Professor);
        var isOdontologo = User.IsInRole(Roles.Odontologo);
        var isProvider = isProfessor || isOdontologo;

        Guid professorId;
        if (isProvider)
        {
            professorId = userId;
        }
        else
        {
            if (!request.ProfessorId.HasValue || request.ProfessorId == Guid.Empty)
            {
                return BadRequest(new { message = "Debe seleccionar un profesor/odontólogo." });
            }
            professorId = request.ProfessorId.Value;
        }

        Guid studentId;
        if (!request.StudentId.HasValue || request.StudentId == Guid.Empty)
        {
            if (!isProvider)
            {
                studentId = userId;
            }
            else if (isOdontologo)
            {
                studentId = userId;
            }
            else
            {
                return BadRequest(new { message = "Debe seleccionar un alumno." });
            }
        }
        else
        {
            studentId = request.StudentId.Value;
        }

        // Validación para alumno
        if (!isProvider && studentId != userId)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.PatientName))
        {
            return BadRequest(new { message = "El nombre del paciente es requerido." });
        }

        var student = await EnsureUserAsync(studentId);
        var professor = await EnsureUserAsync(professorId);

        if (student == null)
        {
            return BadRequest(new { message = $"Usuario con ID {studentId} no encontrado." });
        }
        if (professor == null)
        {
            return BadRequest(new { message = $"Odontólogo con ID {professorId} no encontrado." });
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            ProfessorId = professorId,
            PatientName = request.PatientName,
            Reason = request.Reason,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Notes = request.Notes,
            Status = isProvider ? (request.Status ?? AppointmentStatus.Pending) : AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        await CreateDefaultRemindersAsync(appointment, student, professor);

        appointment.Student = student;
        appointment.Professor = professor;
        return Ok(Map(appointment));
    }

    [Authorize]
    [HttpGet("availability")]
    public async Task<ActionResult<AvailabilityResponse>> GetAvailability([FromQuery] DateTime date, [FromQuery] Guid? professorId, [FromQuery] Guid? studentId)
    {
        var userId = GetUserId();
        var isProvider = User.IsInRole(Roles.Professor) || User.IsInRole(Roles.Odontologo);

        if (!isProvider)
        {
            studentId = userId;
        }
        else
        {
            professorId ??= userId;
        }

        var dayStart = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var query = _db.Appointments.AsNoTracking().Where(x => x.StartAt >= dayStart && x.StartAt < dayEnd);
        if (professorId.HasValue)
        {
            query = query.Where(x => x.ProfessorId == professorId.Value);
        }
        if (studentId.HasValue)
        {
            query = query.Where(x => x.StudentId == studentId.Value);
        }

        var appointments = await query.ToListAsync();
        var slots = BuildSlots(dayStart, appointments);

        return Ok(new AvailabilityResponse
        {
            Date = dayStart,
            Slots = slots
        });
    }

    [Authorize]
    [HttpPut("appointments/{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> UpdateAppointment(Guid id, [FromBody] AppointmentUpdateRequest request)
    {
        var userId = GetUserId();
        var isProvider = User.IsInRole(Roles.Professor) || User.IsInRole(Roles.Odontologo);

        var appointment = await _db.Appointments
            .Include(x => x.Student)
            .Include(x => x.Professor)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (appointment == null)
        {
            return NotFound();
        }

        // Verificar permisos
        if (!isProvider && appointment.StudentId != userId)
        {
            return Forbid();
        }

        if (isProvider && appointment.ProfessorId != userId)
        {
            return Forbid();
        }

        if (!isProvider && appointment.Status != AppointmentStatus.Pending && appointment.Status != AppointmentStatus.Cancelled)
        {
            return BadRequest(new { message = "Solo puedes modificar citas pendientes." });
        }

        // Actualizar campos si se proporcionan
        if (!string.IsNullOrWhiteSpace(request.PatientName))
        {
            appointment.PatientName = request.PatientName;
        }
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            appointment.Reason = request.Reason;
        }
        if (request.Notes != null)
        {
            appointment.Notes = request.Notes;
        }
        if (request.Status.HasValue && isProvider)
        {
            appointment.Status = request.Status.Value;
        }

        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(Map(appointment));
    }

    [Authorize(Roles = $"{Roles.Professor},{Roles.Odontologo}")]
    [HttpPost("appointments/{id:guid}/review")]
    public async Task<ActionResult<AppointmentDto>> ReviewAppointment(Guid id, [FromBody] AppointmentReviewRequest request)
    {
        var userId = GetUserId();

        var appointment = await _db.Appointments
            .Include(x => x.Student)
            .Include(x => x.Professor)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (appointment == null)
        {
            return NotFound();
        }

        if (appointment.ProfessorId != userId)
        {
            return Forbid();
        }

        appointment.Status = request.Approved ? AppointmentStatus.Confirmed : AppointmentStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            appointment.Notes = request.Notes;
        }
        appointment.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(Map(appointment));
    }

    [Authorize]
    [HttpDelete("appointments/{id:guid}")]
    public async Task<IActionResult> DeleteAppointment(Guid id)
    {
        var userId = GetUserId();
        var isProvider = User.IsInRole(Roles.Professor) || User.IsInRole(Roles.Odontologo);

        var appointment = await _db.Appointments.FindAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }

        // Verificar permisos
        if (!isProvider && appointment.StudentId != userId)
        {
            return Forbid();
        }

        if (isProvider && appointment.ProfessorId != userId)
        {
            return Forbid();
        }

        // Eliminar recordatorios asociados
        var reminders = await _db.Reminders.Where(r => r.AppointmentId == id).ToListAsync();
        _db.Reminders.RemoveRange(reminders);

        _db.Appointments.Remove(appointment);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static List<TimeSlotDto> BuildSlots(DateTime dayStart, List<Appointment> appointments)
    {
        var slots = new List<TimeSlotDto>();
        var start = dayStart.AddHours(6);  // 6:00 AM
        var end = dayStart.AddHours(18);   // 6:00 PM
        var cursor = start;
        while (cursor < end)
        {
            var slotEnd = cursor.AddHours(1);
            var occupied = appointments.Any(a =>
                (cursor >= a.StartAt && cursor < a.EndAt) ||
                (slotEnd > a.StartAt && slotEnd <= a.EndAt));
            slots.Add(new TimeSlotDto
            {
                StartAt = cursor,
                EndAt = slotEnd,
                IsAvailable = !occupied
            });
            cursor = slotEnd;
        }
        return slots;
    }

    private async Task CreateDefaultRemindersAsync(Appointment appointment, ApplicationUser student, ApplicationUser professor)
    {
        var reminders = new List<Reminder>();
        var reminderTimes = new[]
        {
            appointment.StartAt.AddHours(-24),
            appointment.StartAt.AddHours(-2)
        };

        foreach (var time in reminderTimes)
        {
            reminders.Add(new Reminder
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                Channel = "Email",
                Target = student.Email ?? string.Empty,
                Message = $"Recordatorio: cita el {appointment.StartAt:dd/MM/yyyy HH:mm}",
                ScheduledAt = time,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
            reminders.Add(new Reminder
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointment.Id,
                Channel = "WhatsApp",
                Target = student.PhoneNumber ?? string.Empty,
                Message = $"Recordatorio: cita el {appointment.StartAt:dd/MM/yyyy HH:mm}",
                ScheduledAt = time,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
        }

        _db.Reminders.AddRange(reminders);
        await _db.SaveChangesAsync();
    }

    private Guid GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new UnauthorizedAccessException();
        }
        return Guid.Parse(id);
    }

    private static AppointmentDto Map(Appointment appointment)
    {
        return new AppointmentDto
        {
            Id = appointment.Id,
            StudentId = appointment.StudentId,
            StudentName = appointment.Student?.FullName ?? string.Empty,
            StudentEmail = appointment.Student?.Email ?? string.Empty,
            ProfessorId = appointment.ProfessorId,
            ProfessorName = appointment.Professor?.FullName ?? string.Empty,
            ProfessorEmail = appointment.Professor?.Email ?? string.Empty,
            PatientName = appointment.PatientName,
            Reason = appointment.Reason,
            StartAt = appointment.StartAt,
            EndAt = appointment.EndAt,
            Status = appointment.Status.ToString(),
            Notes = appointment.Notes
        };
    }

    private async Task<ApplicationUser?> EnsureUserAsync(Guid userId)
    {
        var existing = await _db.Users.FindAsync(userId);
        if (existing != null)
        {
            return existing;
        }

        var sourceUser = await _userManager.FindByIdAsync(userId.ToString());
        if (sourceUser == null)
        {
            return null;
        }

        var clone = new ApplicationUser
        {
            Id = sourceUser.Id,
            FullName = sourceUser.FullName,
            UniversityId = sourceUser.UniversityId,
            UserName = sourceUser.UserName,
            NormalizedUserName = sourceUser.NormalizedUserName,
            Email = sourceUser.Email,
            NormalizedEmail = sourceUser.NormalizedEmail,
            EmailConfirmed = sourceUser.EmailConfirmed,
            PasswordHash = sourceUser.PasswordHash,
            SecurityStamp = sourceUser.SecurityStamp,
            ConcurrencyStamp = sourceUser.ConcurrencyStamp,
            PhoneNumber = sourceUser.PhoneNumber,
            PhoneNumberConfirmed = sourceUser.PhoneNumberConfirmed,
            TwoFactorEnabled = sourceUser.TwoFactorEnabled,
            LockoutEnd = sourceUser.LockoutEnd,
            LockoutEnabled = sourceUser.LockoutEnabled,
            AccessFailedCount = sourceUser.AccessFailedCount
        };

        _db.Users.Add(clone);
        await _db.SaveChangesAsync();
        return clone;
    }
}
