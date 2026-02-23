using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models.Odontologia;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Controllers.Odontologia;

[ApiController]
[Route("api/odontologia/telemedicina")]
[Authorize(Roles = Roles.Odontologo)]
public class TelemedicinaController : ControllerBase
{
    private readonly OdontologoDbContext _db;

    public TelemedicinaController(OdontologoDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("sesiones")]
    public async Task<ActionResult<IEnumerable<TelemedicineSessionDto>>> GetSessions(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var odontologoId = GetUserId();

        var query = _db.TelemedicineSessions
            .AsNoTracking()
            .Where(s => s.OdontologoId == odontologoId);

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
            query = query.Where(s => s.ScheduledStartAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            query = query.Where(s => s.ScheduledEndAt <= toUtc);
        }

        var sessions = await query
            .OrderByDescending(s => s.ScheduledStartAt)
            .Select(s => new TelemedicineSessionDto(
                s.Id,
                s.PatientId,
                s.PatientName,
                s.Topic,
                s.MeetingLink,
                s.ScheduledStartAt,
                s.ScheduledEndAt,
                s.StartedAt,
                s.EndedAt,
                s.Status.ToString(),
                _db.TelemedicineMessages.Count(m => m.SessionId == s.Id)))
            .ToListAsync();

        return Ok(sessions);
    }

    [HttpPost("sesiones")]
    public async Task<ActionResult<TelemedicineSessionDto>> CreateSession([FromBody] CreateTelemedicineSessionRequest request)
    {
        var odontologoId = GetUserId();

        var start = NormalizeUtc(request.ScheduledStartAt);
        var end = NormalizeUtc(request.ScheduledEndAt);
        if (end <= start)
        {
            return BadRequest("La hora de fin debe ser mayor que la de inicio.");
        }

        var patientName = request.PatientName;
        if (request.PatientId.HasValue)
        {
            var patient = await _db.OdontologoPatients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.PatientId.Value && p.OdontologoId == odontologoId);

            if (patient == null)
            {
                return BadRequest("Paciente no encontrado.");
            }

            patientName = $"{patient.FirstName} {patient.LastName}".Trim();
        }

        if (string.IsNullOrWhiteSpace(patientName))
        {
            return BadRequest("Debe indicar un paciente.");
        }

        var session = new TelemedicineSession
        {
            Id = Guid.NewGuid(),
            OdontologoId = odontologoId,
            PatientId = request.PatientId,
            PatientName = patientName.Trim(),
            Topic = request.Topic.Trim(),
            MeetingLink = string.IsNullOrWhiteSpace(request.MeetingLink) ? null : request.MeetingLink.Trim(),
            ScheduledStartAt = start,
            ScheduledEndAt = end,
            Status = TelemedicineSessionStatus.Scheduled,
            CreatedAt = DateTimeHelper.Now(),
            UpdatedAt = DateTimeHelper.Now()
        };

        _db.TelemedicineSessions.Add(session);
        await _db.SaveChangesAsync();

        return Ok(new TelemedicineSessionDto(
            session.Id,
            session.PatientId,
            session.PatientName,
            session.Topic,
            session.MeetingLink,
            session.ScheduledStartAt,
            session.ScheduledEndAt,
            session.StartedAt,
            session.EndedAt,
            session.Status.ToString(),
            0));
    }

    [HttpPost("sesiones/{id:guid}/start")]
    public async Task<ActionResult<TelemedicineSessionDto>> StartSession(Guid id)
    {
        var odontologoId = GetUserId();

        var session = await _db.TelemedicineSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.OdontologoId == odontologoId);

        if (session == null)
        {
            return NotFound();
        }

        session.Status = TelemedicineSessionStatus.InProgress;
        session.StartedAt = DateTimeHelper.Now();
        session.UpdatedAt = DateTimeHelper.Now();
        await _db.SaveChangesAsync();

        var messageCount = await _db.TelemedicineMessages.CountAsync(m => m.SessionId == id);

        return Ok(new TelemedicineSessionDto(
            session.Id,
            session.PatientId,
            session.PatientName,
            session.Topic,
            session.MeetingLink,
            session.ScheduledStartAt,
            session.ScheduledEndAt,
            session.StartedAt,
            session.EndedAt,
            session.Status.ToString(),
            messageCount));
    }

    [HttpPost("sesiones/{id:guid}/end")]
    public async Task<ActionResult<TelemedicineSessionDto>> EndSession(Guid id)
    {
        var odontologoId = GetUserId();

        var session = await _db.TelemedicineSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.OdontologoId == odontologoId);

        if (session == null)
        {
            return NotFound();
        }

        session.Status = TelemedicineSessionStatus.Completed;
        session.EndedAt = DateTimeHelper.Now();
        if (!session.StartedAt.HasValue)
        {
            session.StartedAt = session.EndedAt;
        }
        session.UpdatedAt = DateTimeHelper.Now();
        await _db.SaveChangesAsync();

        var messageCount = await _db.TelemedicineMessages.CountAsync(m => m.SessionId == id);

        return Ok(new TelemedicineSessionDto(
            session.Id,
            session.PatientId,
            session.PatientName,
            session.Topic,
            session.MeetingLink,
            session.ScheduledStartAt,
            session.ScheduledEndAt,
            session.StartedAt,
            session.EndedAt,
            session.Status.ToString(),
            messageCount));
    }

    [HttpPost("sesiones/{id:guid}/cancel")]
    public async Task<IActionResult> CancelSession(Guid id)
    {
        var odontologoId = GetUserId();

        var session = await _db.TelemedicineSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.OdontologoId == odontologoId);

        if (session == null)
        {
            return NotFound();
        }

        session.Status = TelemedicineSessionStatus.Cancelled;
        session.UpdatedAt = DateTimeHelper.Now();

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("sesiones/{id:guid}/mensajes")]
    public async Task<ActionResult<IEnumerable<TelemedicineMessageDto>>> GetMessages(Guid id)
    {
        var odontologoId = GetUserId();
        var exists = await _db.TelemedicineSessions.AnyAsync(s => s.Id == id && s.OdontologoId == odontologoId);
        if (!exists)
        {
            return NotFound();
        }

        var messages = await _db.TelemedicineMessages
            .AsNoTracking()
            .Where(m => m.SessionId == id)
            .OrderBy(m => m.SentAt)
            .Select(m => new TelemedicineMessageDto(m.Id, m.SessionId, m.SenderRole, m.SenderName, m.Message, m.SentAt))
            .ToListAsync();

        return Ok(messages);
    }

    [HttpPost("sesiones/{id:guid}/mensajes")]
    public async Task<ActionResult<TelemedicineMessageDto>> AddMessage(Guid id, [FromBody] AddTelemedicineMessageRequest request)
    {
        var odontologoId = GetUserId();
        var session = await _db.TelemedicineSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.OdontologoId == odontologoId);

        if (session == null)
        {
            return NotFound();
        }

        var senderName = string.IsNullOrWhiteSpace(request.SenderName)
            ? (User.FindFirstValue(ClaimTypes.Name) ?? "Odontólogo")
            : request.SenderName.Trim();

        var message = new TelemedicineMessage
        {
            Id = Guid.NewGuid(),
            SessionId = id,
            SenderRole = string.IsNullOrWhiteSpace(request.SenderRole) ? Roles.Odontologo : request.SenderRole.Trim(),
            SenderName = senderName,
            Message = request.Message.Trim(),
            SentAt = DateTimeHelper.Now()
        };

        _db.TelemedicineMessages.Add(message);
        session.UpdatedAt = DateTimeHelper.Now();
        await _db.SaveChangesAsync();

        return Ok(new TelemedicineMessageDto(message.Id, message.SessionId, message.SenderRole, message.SenderName, message.Message, message.SentAt));
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}

public record TelemedicineSessionDto(
    Guid Id,
    Guid? PatientId,
    string PatientName,
    string Topic,
    string? MeetingLink,
    DateTime ScheduledStartAt,
    DateTime ScheduledEndAt,
    DateTime? StartedAt,
    DateTime? EndedAt,
    string Status,
    int MessageCount);

public record TelemedicineMessageDto(
    Guid Id,
    Guid SessionId,
    string SenderRole,
    string SenderName,
    string Message,
    DateTime SentAt);

public record CreateTelemedicineSessionRequest(
    Guid? PatientId,
    string PatientName,
    string Topic,
    string? MeetingLink,
    DateTime ScheduledStartAt,
    DateTime ScheduledEndAt);

public record AddTelemedicineMessageRequest(
    string Message,
    string? SenderRole,
    string? SenderName);
