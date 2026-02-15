using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Models.Academico;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers.Academico;

[ApiController]
[Route("api/academic/clinical-histories")]
[Authorize(Roles = $"{Roles.Student},{Roles.Professor}")]
public class AcademicClinicalHistoriesController : ControllerBase
{
    private readonly AcademicDbContext _db;

    public AcademicClinicalHistoriesController(AcademicDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AcademicClinicalHistoryDto>>> GetAll([FromQuery] Guid? studentId, [FromQuery] string? status)
    {
        var userId = GetUserId();
        var isProfessor = User.IsInRole(Roles.Professor);

        var query = _db.AcademicClinicalHistories
            .Include(h => h.Student)
            .Include(h => h.ReviewedByProfessor)
            .AsNoTracking();

        // Alumno solo ve las suyas, Profesor ve todas
        if (!isProfessor)
        {
            query = query.Where(h => h.StudentId == userId);
        }
        else if (studentId.HasValue)
        {
            query = query.Where(h => h.StudentId == studentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ClinicalHistoryStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(h => h.Status == parsedStatus);
        }

        var histories = await query
            .OrderByDescending(h => h.UpdatedAt)
            .ToListAsync();

        return Ok(histories.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AcademicClinicalHistoryDto>> GetById(Guid id)
    {
        var userId = GetUserId();
        var isProfessor = User.IsInRole(Roles.Professor);

        var history = await _db.AcademicClinicalHistories
            .Include(h => h.Student)
            .Include(h => h.ReviewedByProfessor)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (history == null)
            return NotFound();

        if (history.Status != ClinicalHistoryStatus.Submitted)
        {
            return BadRequest(new { message = "La historia clínica debe estar enviada para revisión." });
        }

        // Alumno solo puede ver las suyas
        if (!isProfessor && history.StudentId != userId)
            return Forbid();

        return Ok(MapToDto(history));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Student)]
    public async Task<ActionResult<AcademicClinicalHistoryDto>> Create([FromBody] CreateAcademicClinicalHistoryRequest request)
    {
        var studentId = GetUserId();

        var history = new AcademicClinicalHistory
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            Data = request.Data,
            Status = ClinicalHistoryStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.AcademicClinicalHistories.Add(history);
        await _db.SaveChangesAsync();

        history.Student = (await _db.Users.FindAsync(studentId))!;

        return CreatedAtAction(nameof(GetById), new { id = history.Id }, MapToDto(history));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AcademicClinicalHistoryDto>> Update(Guid id, [FromBody] UpdateAcademicClinicalHistoryRequest request)
    {
        var userId = GetUserId();
        var isProfessor = User.IsInRole(Roles.Professor);

        var history = await _db.AcademicClinicalHistories
            .Include(h => h.Student)
            .Include(h => h.ReviewedByProfessor)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (history == null)
            return NotFound();

        // Alumno solo puede editar las suyas si están en Draft o Rejected
        if (!isProfessor)
        {
            if (history.StudentId != userId)
                return Forbid();

            if (history.Status == ClinicalHistoryStatus.Submitted || history.Status == ClinicalHistoryStatus.Approved)
            {
                return BadRequest(new { message = "Solo se pueden editar historias en estado Draft o Rejected" });
            }
        }

        history.Data = request.Data;
        history.UpdatedAt = DateTime.UtcNow;

        if (!isProfessor && history.Status == ClinicalHistoryStatus.Rejected)
        {
            history.Status = ClinicalHistoryStatus.Draft;
            history.ReviewedAt = null;
            history.ReviewedByProfessorId = null;
            history.ProfessorComments = null;
        }

        await _db.SaveChangesAsync();

        return Ok(MapToDto(history));
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = Roles.Student)]
    public async Task<ActionResult<AcademicClinicalHistoryDto>> Submit(Guid id)
    {
        var studentId = GetUserId();

        var history = await _db.AcademicClinicalHistories
            .Include(h => h.Student)
            .Include(h => h.ReviewedByProfessor)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (history == null)
        {
            return NotFound();
        }

        if (history.StudentId != studentId)
        {
            return Forbid();
        }

        if (history.Status != ClinicalHistoryStatus.Draft)
        {
            return BadRequest(new { message = "La historia clínica debe estar en estado Draft para enviar." });
        }

        history.Status = ClinicalHistoryStatus.Submitted;
        history.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(MapToDto(history));
    }

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = Roles.Professor)]
    public async Task<ActionResult<AcademicClinicalHistoryDto>> Review(Guid id, [FromBody] ReviewAcademicClinicalHistoryRequest request)
    {
        var professorId = GetUserId();

        var history = await _db.AcademicClinicalHistories
            .Include(h => h.Student)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (history == null)
            return NotFound();

        history.ReviewedByProfessorId = professorId;
        history.ProfessorComments = request.ReviewNotes;
        history.Status = request.Approved ? ClinicalHistoryStatus.Approved : ClinicalHistoryStatus.Rejected;
        history.ReviewedAt = DateTime.UtcNow;
        history.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        history.ReviewedByProfessor = await _db.Users.FindAsync(professorId);

        return Ok(MapToDto(history));
    }

    private static AcademicClinicalHistoryDto MapToDto(AcademicClinicalHistory h) => new(
        h.Id,
        h.StudentId,
        h.Student?.FullName ?? "",
        h.Data,
        h.Status.ToString(),
        h.ReviewedByProfessorId,
        h.ReviewedByProfessor?.FullName,
        h.ProfessorComments,
        h.ReviewedAt,
        h.CreatedAt,
        h.UpdatedAt
    );
}

public record CreateAcademicClinicalHistoryRequest(JsonObject Data);

public record UpdateAcademicClinicalHistoryRequest(JsonObject Data);

public record ReviewAcademicClinicalHistoryRequest(
    bool Approved,
    [property: JsonPropertyName("reviewNotes")] string? ReviewNotes);

public record AcademicClinicalHistoryDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    JsonObject Data,
    string Status,
    Guid? ReviewedByProfessorId,
    string? ReviewedByProfessorName,
    string? ProfessorComments,
    DateTime? ReviewedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
