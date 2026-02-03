using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers;

[ApiController]
[Route("api/reminders")]
public class RemindersController : ControllerBase
{
    private readonly AppDbContext _db;

    public RemindersController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReminderDto>>> GetReminders([FromQuery] string? status)
    {
        var userId = GetUserId();
        var isProvider = User.IsInRole(Roles.Professor) || User.IsInRole(Roles.Odontologo);

        var query = _db.Reminders
            .Include(r => r.Appointment)
            .AsNoTracking();

        if (!isProvider)
        {
            query = query.Where(r => r.Appointment.StudentId == userId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        var reminders = await query.OrderBy(r => r.ScheduledAt).ToListAsync();
        return Ok(reminders.Select(r => new ReminderDto
        {
            Id = r.Id,
            AppointmentId = r.AppointmentId,
            Channel = r.Channel,
            Target = r.Target,
            Message = r.Message,
            ScheduledAt = r.ScheduledAt,
            Status = r.Status
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

public class ReminderDto
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
