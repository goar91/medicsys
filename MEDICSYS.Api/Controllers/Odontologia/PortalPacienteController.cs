using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Models.Odontologia;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Controllers.Odontologia;

[ApiController]
[Route("api/odontologia/portal-paciente")]
[Authorize(Roles = Roles.Odontologo)]
public class PortalPacienteController : ControllerBase
{
    private readonly OdontologoDbContext _db;

    public PortalPacienteController(OdontologoDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IQueryable<Invoice> GetOwnedInvoices(Guid odontologoId)
    {
        return _db.Invoices.Where(i => _db.OdontologoInvoiceOwnerships
            .Any(o => o.InvoiceId == i.Id && o.OdontologoId == odontologoId));
    }

    [HttpGet("pacientes")]
    public async Task<ActionResult<IEnumerable<object>>> GetPatients()
    {
        var odontologoId = GetUserId();
        var patients = await _db.OdontologoPatients
            .AsNoTracking()
            .Where(p => p.OdontologoId == odontologoId)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new
            {
                p.Id,
                FullName = $"{p.FirstName} {p.LastName}".Trim(),
                p.IdNumber,
                p.Phone,
                p.Email
            })
            .ToListAsync();

        return Ok(patients);
    }

    [HttpGet("pacientes/{patientId:guid}/resumen")]
    public async Task<ActionResult<object>> GetPatientSummary(Guid patientId)
    {
        var odontologoId = GetUserId();

        var patient = await _db.OdontologoPatients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId && p.OdontologoId == odontologoId);

        if (patient == null)
        {
            return NotFound();
        }

        var fullName = $"{patient.FirstName} {patient.LastName}".Trim();
        var now = DateTimeHelper.Now();

        var upcomingAppointments = await _db.OdontologoAppointments
            .AsNoTracking()
            .Where(a =>
                a.OdontologoId == odontologoId &&
                a.StartAt >= now &&
                (a.PatientName == fullName || a.PatientName == patient.FirstName || a.PatientName == patient.LastName))
            .OrderBy(a => a.StartAt)
            .Take(10)
            .Select(a => new
            {
                a.Id,
                a.StartAt,
                a.EndAt,
                a.Reason,
                Status = a.Status.ToString()
            })
            .ToListAsync();

        var invoices = await GetOwnedInvoices(odontologoId)
            .AsNoTracking()
            .Where(i => i.CustomerIdentification == patient.IdNumber || i.CustomerName == fullName)
            .OrderByDescending(i => i.IssuedAt)
            .Take(20)
            .Select(i => new
            {
                i.Id,
                i.Number,
                i.IssuedAt,
                i.TotalToCharge,
                Status = i.Status.ToString()
            })
            .ToListAsync();

        var histories = await _db.OdontologoClinicalHistories
            .AsNoTracking()
            .Where(h => h.OdontologoId == odontologoId && h.PatientIdNumber == patient.IdNumber)
            .OrderByDescending(h => h.CreatedAt)
            .Take(20)
            .Select(h => new
            {
                h.Id,
                h.CreatedAt,
                h.UpdatedAt,
                Status = h.Status.ToString()
            })
            .ToListAsync();

        var preference = await _db.PatientPortalPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OdontologoId == odontologoId && p.PatientId == patientId);

        return Ok(new
        {
            Patient = new
            {
                patient.Id,
                FullName = fullName,
                patient.IdNumber,
                patient.Email,
                patient.Phone
            },
            UpcomingAppointments = upcomingAppointments,
            Invoices = invoices,
            ClinicalHistoryEntries = histories,
            Preferences = new
            {
                EmailEnabled = preference?.EmailEnabled ?? true,
                WhatsAppEnabled = preference?.WhatsAppEnabled ?? false,
                UpdatedAt = preference?.UpdatedAt
            }
        });
    }

    [HttpGet("pacientes/{patientId:guid}/preferencias")]
    public async Task<ActionResult<object>> GetPreferences(Guid patientId)
    {
        var odontologoId = GetUserId();

        var patientExists = await _db.OdontologoPatients
            .AsNoTracking()
            .AnyAsync(p => p.Id == patientId && p.OdontologoId == odontologoId);

        if (!patientExists)
        {
            return NotFound();
        }

        var preference = await EnsurePreferenceAsync(odontologoId, patientId);

        return Ok(new
        {
            preference.Id,
            preference.PatientId,
            preference.EmailEnabled,
            preference.WhatsAppEnabled,
            preference.UpdatedAt
        });
    }

    [HttpPut("pacientes/{patientId:guid}/preferencias")]
    public async Task<ActionResult<object>> UpdatePreferences(Guid patientId, [FromBody] UpdatePortalPreferenceRequest request)
    {
        var odontologoId = GetUserId();

        var patientExists = await _db.OdontologoPatients
            .AsNoTracking()
            .AnyAsync(p => p.Id == patientId && p.OdontologoId == odontologoId);

        if (!patientExists)
        {
            return NotFound();
        }

        var preference = await EnsurePreferenceAsync(odontologoId, patientId);
        preference.EmailEnabled = request.EmailEnabled;
        preference.WhatsAppEnabled = request.WhatsAppEnabled;
        preference.UpdatedAt = DateTimeHelper.Now();
        await _db.SaveChangesAsync();

        return Ok(new
        {
            preference.Id,
            preference.PatientId,
            preference.EmailEnabled,
            preference.WhatsAppEnabled,
            preference.UpdatedAt
        });
    }

    [HttpPost("pacientes/{patientId:guid}/recordatorios")]
    public async Task<ActionResult<IEnumerable<object>>> ScheduleReminder(Guid patientId, [FromBody] SchedulePortalReminderRequest request)
    {
        var odontologoId = GetUserId();
        var patient = await _db.OdontologoPatients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId && p.OdontologoId == odontologoId);

        if (patient == null)
        {
            return NotFound();
        }

        var preference = await EnsurePreferenceAsync(odontologoId, patientId);

        var channels = new List<string>();
        if (request.SendEmail && preference.EmailEnabled && !string.IsNullOrWhiteSpace(patient.Email))
        {
            channels.Add("Email");
        }
        if (request.SendWhatsApp && preference.WhatsAppEnabled && !string.IsNullOrWhiteSpace(patient.Phone))
        {
            channels.Add("WhatsApp");
        }

        if (channels.Count == 0)
        {
            return BadRequest("No hay canales disponibles para el recordatorio.");
        }

        var scheduleAt = NormalizeUtc(request.ScheduledFor ?? DateTimeHelper.Now());

        var notifications = new List<PatientPortalNotification>();
        foreach (var channel in channels)
        {
            notifications.Add(new PatientPortalNotification
            {
                Id = Guid.NewGuid(),
                OdontologoId = odontologoId,
                PatientId = patientId,
                Channel = channel,
                Subject = string.IsNullOrWhiteSpace(request.Subject) ? "Recordatorio de cita" : request.Subject.Trim(),
                Message = request.Message.Trim(),
                ScheduledFor = scheduleAt,
                Status = PatientPortalNotificationStatus.Pending,
                ExternalReference = null
            });
        }

        _db.PatientPortalNotifications.AddRange(notifications);
        await _db.SaveChangesAsync();

        return Ok(notifications.Select(n => new
        {
            n.Id,
            n.Channel,
            n.Subject,
            n.Message,
            n.ScheduledFor,
            Status = n.Status.ToString()
        }));
    }

    [HttpGet("notificaciones")]
    public async Task<ActionResult<IEnumerable<object>>> GetNotifications([FromQuery] Guid? patientId, [FromQuery] string? status)
    {
        var odontologoId = GetUserId();

        var query = _db.PatientPortalNotifications
            .AsNoTracking()
            .Where(n => n.OdontologoId == odontologoId);

        if (patientId.HasValue)
        {
            query = query.Where(n => n.PatientId == patientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PatientPortalNotificationStatus>(status, true, out var parsed))
        {
            query = query.Where(n => n.Status == parsed);
        }

        var notifications = await query
            .OrderByDescending(n => n.ScheduledFor)
            .Take(100)
            .ToListAsync();

        return Ok(notifications.Select(n => new
        {
            n.Id,
            n.PatientId,
            n.Channel,
            n.Subject,
            n.Message,
            n.ScheduledFor,
            n.SentAt,
            Status = n.Status.ToString()
        }));
    }

    [HttpPost("notificaciones/{id:guid}/marcar-enviado")]
    public async Task<IActionResult> MarkNotificationAsSent(Guid id)
    {
        var odontologoId = GetUserId();

        var notification = await _db.PatientPortalNotifications
            .FirstOrDefaultAsync(n => n.Id == id && n.OdontologoId == odontologoId);

        if (notification == null)
        {
            return NotFound();
        }

        notification.Status = PatientPortalNotificationStatus.Sent;
        notification.SentAt = DateTimeHelper.Now();
        notification.ExternalReference = $"SIM-{DateTimeHelper.Now():yyyyMMddHHmmss}";

        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<PatientPortalPreference> EnsurePreferenceAsync(Guid odontologoId, Guid patientId)
    {
        var preference = await _db.PatientPortalPreferences
            .FirstOrDefaultAsync(p => p.OdontologoId == odontologoId && p.PatientId == patientId);

        if (preference != null)
        {
            return preference;
        }

        preference = new PatientPortalPreference
        {
            Id = Guid.NewGuid(),
            OdontologoId = odontologoId,
            PatientId = patientId,
            EmailEnabled = true,
            WhatsAppEnabled = false,
            UpdatedAt = DateTimeHelper.Now()
        };

        _db.PatientPortalPreferences.Add(preference);
        await _db.SaveChangesAsync();
        return preference;
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

public record UpdatePortalPreferenceRequest(bool EmailEnabled, bool WhatsAppEnabled);

public record SchedulePortalReminderRequest(
    string Message,
    string? Subject,
    DateTime? ScheduledFor,
    bool SendEmail,
    bool SendWhatsApp);
