using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Models.Odontologia;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers.Odontologia;

[ApiController]
[Route("api/odontologia/appointments")]
[Authorize(Roles = Roles.Odontologo)]
public class OdontologoAppointmentsController : ControllerBase
{
    private readonly OdontologoDbContext _db;

    public OdontologoAppointmentsController(OdontologoDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OdontologoAppointmentDto>>> GetAll([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        var odontologoId = GetUserId();

        var query = _db.OdontologoAppointments
            .Where(a => a.OdontologoId == odontologoId)
            .AsNoTracking();

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
    public async Task<ActionResult<OdontologoAppointmentDto>> Create([FromBody] CreateOdontologoAppointmentRequest request)
    {
        var odontologoId = GetUserId();

        var appointment = new OdontologoAppointment
        {
            Id = Guid.NewGuid(),
            OdontologoId = odontologoId,
            PatientName = request.PatientName,
            Reason = request.Reason,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            Notes = request.Notes,
            Status = request.Status ?? AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.OdontologoAppointments.Add(appointment);
        await _db.SaveChangesAsync();

        return Ok(MapToDto(appointment));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OdontologoAppointmentDto>> Update(Guid id, [FromBody] UpdateOdontologoAppointmentRequest request)
    {
        var odontologoId = GetUserId();

        var appointment = await _db.OdontologoAppointments
            .FirstOrDefaultAsync(a => a.Id == id && a.OdontologoId == odontologoId);

        if (appointment == null)
            return NotFound();

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
        var odontologoId = GetUserId();

        var appointment = await _db.OdontologoAppointments
            .FirstOrDefaultAsync(a => a.Id == id && a.OdontologoId == odontologoId);

        if (appointment == null)
            return NotFound();

        _db.OdontologoAppointments.Remove(appointment);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static OdontologoAppointmentDto MapToDto(OdontologoAppointment a) => new(
        a.Id,
        a.PatientName,
        a.Reason,
        a.StartAt,
        a.EndAt,
        a.Notes,
        a.Status.ToString(),
        a.CreatedAt
    );
}

public record CreateOdontologoAppointmentRequest(
    string PatientName,
    string Reason,
    DateTime StartAt,
    DateTime EndAt,
    string? Notes,
    AppointmentStatus? Status
);

public record UpdateOdontologoAppointmentRequest(
    string PatientName,
    string Reason,
    string? Notes,
    AppointmentStatus? Status
);

public record OdontologoAppointmentDto(
    Guid Id,
    string PatientName,
    string Reason,
    DateTime StartAt,
    DateTime EndAt,
    string? Notes,
    string Status,
    DateTime CreatedAt
);
