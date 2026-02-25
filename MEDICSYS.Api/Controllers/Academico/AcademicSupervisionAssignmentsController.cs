using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Models.Academico;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Controllers.Academico;

[ApiController]
[Route("api/academic/supervision-assignments")]
[Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
public class AcademicSupervisionAssignmentsController : ControllerBase
{
    private readonly AcademicDbContext _db;

    public AcademicSupervisionAssignmentsController(AcademicDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AcademicSupervisionAssignmentDto>>> GetAll(
        [FromQuery] Guid? professorId,
        [FromQuery] Guid? studentId,
        [FromQuery] Guid? patientId,
        [FromQuery] bool includeInactive = false)
    {
        var actorId = GetUserId();
        var isAdmin = User.IsInRole(Roles.Admin);

        var query = _db.AcademicSupervisionAssignments
            .Include(a => a.Professor)
            .Include(a => a.Student)
            .Include(a => a.Patient)
            .Include(a => a.AssignedByUser)
            .AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(a => a.IsActive);
        }

        if (!isAdmin)
        {
            query = query.Where(a => a.ProfessorId == actorId);
        }
        else if (professorId.HasValue)
        {
            query = query.Where(a => a.ProfessorId == professorId.Value);
        }

        if (studentId.HasValue)
        {
            query = query.Where(a => a.StudentId == studentId.Value);
        }

        if (patientId.HasValue)
        {
            query = query.Where(a => a.PatientId == patientId.Value);
        }

        var items = await query
            .OrderByDescending(a => a.IsActive)
            .ThenByDescending(a => a.AssignedAt)
            .ToListAsync();

        return Ok(items.Select(MapToDto));
    }

    [HttpPost]
    public async Task<ActionResult<AcademicSupervisionAssignmentDto>> Create([FromBody] CreateAcademicSupervisionAssignmentRequest request)
    {
        var actorId = GetUserId();
        var isAdmin = User.IsInRole(Roles.Admin);

        if (request.StudentId == Guid.Empty)
        {
            return BadRequest(new { message = "Debe seleccionar un estudiante." });
        }

        var professorId = request.ProfessorId ?? actorId;
        if (professorId == Guid.Empty)
        {
            return BadRequest(new { message = "Debe seleccionar un profesor." });
        }

        if (!isAdmin && professorId != actorId)
        {
            return Forbid();
        }

        if (!await HasRoleAsync(professorId, Roles.Professor))
        {
            return BadRequest(new { message = "El usuario seleccionado no tiene rol Profesor." });
        }

        if (!await HasRoleAsync(request.StudentId, Roles.Student))
        {
            return BadRequest(new { message = "El usuario seleccionado no tiene rol Alumno." });
        }

        if (request.PatientId.HasValue && request.PatientId.Value != Guid.Empty)
        {
            var patient = await _db.AcademicPatients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.PatientId.Value);
            if (patient == null)
            {
                return NotFound(new { message = "Paciente no encontrado." });
            }

            if (!isAdmin && patient.CreatedByProfessorId != professorId)
            {
                return Forbid();
            }
        }

        Guid? patientId = request.PatientId.HasValue && request.PatientId.Value != Guid.Empty
            ? request.PatientId.Value
            : null;
        var alreadyExists = await _db.AcademicSupervisionAssignments.AnyAsync(a =>
            a.IsActive
            && a.ProfessorId == professorId
            && a.StudentId == request.StudentId
            && a.PatientId == patientId);

        if (alreadyExists)
        {
            return Conflict(new { message = "La asignación activa ya existe." });
        }

        var now = DateTimeHelper.Now();
        var item = new AcademicSupervisionAssignment
        {
            Id = Guid.NewGuid(),
            ProfessorId = professorId,
            StudentId = request.StudentId,
            PatientId = patientId,
            AssignedByUserId = actorId,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            IsActive = true,
            AssignedAt = now,
            UpdatedAt = now
        };

        _db.AcademicSupervisionAssignments.Add(item);
        await _db.SaveChangesAsync();

        item = await _db.AcademicSupervisionAssignments
            .Include(a => a.Professor)
            .Include(a => a.Student)
            .Include(a => a.Patient)
            .Include(a => a.AssignedByUser)
            .FirstAsync(a => a.Id == item.Id);

        return CreatedAtAction(nameof(GetAll), new { id = item.Id }, MapToDto(item));
    }

    [HttpPut("{id:guid}/deactivate")]
    public async Task<ActionResult<AcademicSupervisionAssignmentDto>> Deactivate(Guid id, [FromBody] UpdateAssignmentStatusRequest? request = null)
    {
        var actorId = GetUserId();
        var isAdmin = User.IsInRole(Roles.Admin);

        var item = await _db.AcademicSupervisionAssignments
            .Include(a => a.Professor)
            .Include(a => a.Student)
            .Include(a => a.Patient)
            .Include(a => a.AssignedByUser)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (item == null)
        {
            return NotFound();
        }

        if (!isAdmin && item.ProfessorId != actorId)
        {
            return Forbid();
        }

        item.IsActive = false;
        if (!string.IsNullOrWhiteSpace(request?.Notes))
        {
            item.Notes = request.Notes.Trim();
        }
        item.UpdatedAt = DateTimeHelper.Now();

        await _db.SaveChangesAsync();
        return Ok(MapToDto(item));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<bool> HasRoleAsync(Guid userId, string roleName)
    {
        return await _db.UserRoles
            .AsNoTracking()
            .Join(_db.Roles.AsNoTracking(),
                ur => ur.RoleId,
                role => role.Id,
                (ur, role) => new { ur.UserId, role.Name })
            .AnyAsync(x => x.UserId == userId && x.Name == roleName);
    }

    private static AcademicSupervisionAssignmentDto MapToDto(AcademicSupervisionAssignment item) => new(
        item.Id,
        item.ProfessorId,
        item.Professor.FullName,
        item.StudentId,
        item.Student.FullName,
        item.PatientId,
        item.Patient == null ? null : $"{item.Patient.FirstName} {item.Patient.LastName}".Trim(),
        item.AssignedByUserId,
        item.AssignedByUser.FullName,
        item.IsActive,
        item.Notes,
        item.AssignedAt,
        item.UpdatedAt
    );
}

public record CreateAcademicSupervisionAssignmentRequest(
    Guid StudentId,
    Guid? ProfessorId,
    Guid? PatientId,
    string? Notes);

public record UpdateAssignmentStatusRequest(string? Notes);

public record AcademicSupervisionAssignmentDto(
    Guid Id,
    Guid ProfessorId,
    string ProfessorName,
    Guid StudentId,
    string StudentName,
    Guid? PatientId,
    string? PatientName,
    Guid AssignedByUserId,
    string AssignedByUserName,
    bool IsActive,
    string? Notes,
    DateTime AssignedAt,
    DateTime UpdatedAt);
