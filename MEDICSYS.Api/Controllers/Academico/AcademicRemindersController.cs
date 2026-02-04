using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers.Academico;

[ApiController]
[Route("api/academic/reminders")]
[Authorize(Roles = $"{Roles.Student},{Roles.Professor}")]
public class AcademicRemindersController : ControllerBase
{
    private readonly AcademicDbContext _db;

    public AcademicRemindersController(AcademicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AcademicReminderDto>>> GetAll([FromQuery] Guid? appointmentId)
    {
        var userId = GetUserId();
        var isProfessor = User.IsInRole(Roles.Professor);

        var query = _db.AcademicReminders
            .Include(r => r.Appointment)
            .AsNoTracking();

        if (!isProfessor)
        {
            query = query.Where(r => r.Appointment.StudentId == userId);
        }

        if (appointmentId.HasValue)
        {
            query = query.Where(r => r.AppointmentId == appointmentId.Value);
        }

        var reminders = await query
            .OrderBy(r => r.ScheduledAt)
            .ToListAsync();

        return Ok(reminders.Select(r => new AcademicReminderDto
        {
            Id = r.Id,
            AppointmentId = r.AppointmentId,
            Target = r.Target,
            Message = r.Message,
            Channel = r.Channel,
            ScheduledAt = r.ScheduledAt,
            Status = r.Status,
            CreatedAt = r.CreatedAt
        }));
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
}

public class AcademicReminderDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string Target { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
