using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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

    public AgendaController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet("appointments")]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAppointments([FromQuery] Guid? studentId, [FromQuery] Guid? professorId)
    {
        var userId = GetUserId();
        var isProfessor = User.IsInRole(Roles.Professor);

        var query = _db.Appointments
            .Include(x => x.Student)
            .Include(x => x.Professor)
            .AsNoTracking();

        if (isProfessor)
        {
            if (studentId.HasValue)
            {
                query = query.Where(x => x.StudentId == studentId.Value);
            }
            if (professorId.HasValue)
            {
                query = query.Where(x => x.ProfessorId == professorId.Value);
            }
        }
        else
        {
            query = query.Where(x => x.StudentId == userId);
        }

        var items = await query.OrderBy(x => x.StartAt).ToListAsync();
        return Ok(items.Select(Map));
    }

    [Authorize]
    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment(AppointmentRequest request)
    {
        var userId = GetUserId();
        var isProfessor = User.IsInRole(Roles.Professor);

        if (!isProfessor && request.StudentId != userId)
        {
            return Forbid();
        }

        var student = await _db.Users.FindAsync(request.StudentId);
        var professor = await _db.Users.FindAsync(request.ProfessorId);
        if (student == null || professor == null)
        {
            return BadRequest("Invalid student or professor.");
        }

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            StudentId = request.StudentId,
            ProfessorId = request.ProfessorId,
            PatientName = request.PatientName,
            Reason = request.Reason,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Notes = request.Notes,
            Status = AppointmentStatus.Scheduled,
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
        var isProfessor = User.IsInRole(Roles.Professor);

        if (!isProfessor)
        {
            studentId = userId;
        }

        var dayStart = date.Date;
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

    private static List<TimeSlotDto> BuildSlots(DateTime dayStart, List<Appointment> appointments)
    {
        var slots = new List<TimeSlotDto>();
        var start = dayStart.AddHours(8);
        var end = dayStart.AddHours(18);
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
}
