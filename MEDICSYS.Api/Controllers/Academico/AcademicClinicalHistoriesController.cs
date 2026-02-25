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
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Controllers.Academico;

[ApiController]
[Route("api/academic/clinical-histories")]
[Authorize(Roles = $"{Roles.Student},{Roles.Professor},{Roles.Admin}")]
public class AcademicClinicalHistoriesController : ControllerBase
{
    private readonly AcademicDbContext _db;
    private readonly AcademicScopeService _scope;

    public AcademicClinicalHistoriesController(AcademicDbContext db, AcademicScopeService scope)
    {
        _db = db;
        _scope = scope;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AcademicClinicalHistoryDto>>> GetAll([FromQuery] Guid? studentId, [FromQuery] string? status)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole(Roles.Admin);
        var isProfessor = User.IsInRole(Roles.Professor);

        var query = _db.AcademicClinicalHistories
            .Include(h => h.Student)
            .Include(h => h.ReviewedByProfessor)
            .AsNoTracking();

        if (!isProfessor && !isAdmin)
        {
            if (studentId.HasValue && studentId.Value != userId)
            {
                return Forbid();
            }
            query = query.Where(h => h.StudentId == userId);
        }
        else if (isProfessor && !isAdmin)
        {
            var supervisedStudentIds = (await _scope.GetSupervisedStudentIdsAsync(userId)).ToList();
            if (studentId.HasValue && !supervisedStudentIds.Contains(studentId.Value))
            {
                return Forbid();
            }

            if (supervisedStudentIds.Count == 0)
            {
                return Ok(Array.Empty<AcademicClinicalHistoryDto>());
            }

            query = query.Where(h => supervisedStudentIds.Contains(h.StudentId));
        }

        if ((isProfessor || isAdmin) && studentId.HasValue)
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
        var isAdmin = User.IsInRole(Roles.Admin);
        var isProfessor = User.IsInRole(Roles.Professor);

        var history = await _db.AcademicClinicalHistories
            .Include(h => h.Student)
            .Include(h => h.ReviewedByProfessor)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (history == null)
        {
            return NotFound();
        }

        if (!isProfessor && !isAdmin && history.StudentId != userId)
        {
            return Forbid();
        }

        if (isProfessor && !isAdmin &&
            !await _scope.ProfessorSupervisesStudentAsync(userId, history.StudentId))
        {
            return Forbid();
        }

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
            Data = request.Data ?? new JsonObject(),
            Status = ClinicalHistoryStatus.Draft,
            CreatedAt = DateTimeHelper.Now(),
            UpdatedAt = DateTimeHelper.Now()
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
        var isAdmin = User.IsInRole(Roles.Admin);
        var isProfessor = User.IsInRole(Roles.Professor);
        var isReviewer = isProfessor || isAdmin;

        var history = await _db.AcademicClinicalHistories
            .Include(h => h.Student)
            .Include(h => h.ReviewedByProfessor)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (history == null)
        {
            return NotFound();
        }

        if (!isReviewer)
        {
            if (history.StudentId != userId)
            {
                return Forbid();
            }

            if (history.Status == ClinicalHistoryStatus.Submitted || history.Status == ClinicalHistoryStatus.Approved)
            {
                return BadRequest(new { message = "Solo se pueden editar historias en estado Draft o Rejected" });
            }
        }
        else if (isProfessor && !isAdmin &&
                 !await _scope.ProfessorSupervisesStudentAsync(userId, history.StudentId))
        {
            return Forbid();
        }

        history.Data = request.Data ?? new JsonObject();
        history.UpdatedAt = DateTimeHelper.Now();

        if (!isProfessor && history.Status == ClinicalHistoryStatus.Rejected)
        {
            history.Status = ClinicalHistoryStatus.Draft;
            history.SubmittedAt = null;
            history.ReviewedAt = null;
            history.ReviewedByProfessorId = null;
            history.ProfessorComments = null;
            history.Grade = null;
        }

        await _db.SaveChangesAsync();

        return Ok(MapToDto(history));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var isAdmin = User.IsInRole(Roles.Admin);

        var history = await _db.AcademicClinicalHistories.FirstOrDefaultAsync(h => h.Id == id);
        if (history == null)
        {
            return NotFound();
        }

        if (!isAdmin && !await _scope.ProfessorSupervisesStudentAsync(userId, history.StudentId))
        {
            return Forbid();
        }

        _db.AcademicClinicalHistories.Remove(history);
        await _db.SaveChangesAsync();
        return NoContent();
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
        history.SubmittedAt = DateTimeHelper.Now();
        history.UpdatedAt = DateTimeHelper.Now();

        await _db.SaveChangesAsync();

        return Ok(MapToDto(history));
    }

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult<AcademicClinicalHistoryDto>> Review(Guid id, [FromBody] ReviewAcademicClinicalHistoryRequest request)
    {
        var professorId = GetUserId();
        var isAdmin = User.IsInRole(Roles.Admin);

        if (!IsValidGrade(request.Grade))
        {
            return BadRequest(new { message = "La calificación debe estar entre 0 y 10." });
        }

        var history = await _db.AcademicClinicalHistories
            .Include(h => h.Student)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (history == null)
        {
            return NotFound();
        }

        if (!isAdmin && !await _scope.ProfessorSupervisesStudentAsync(professorId, history.StudentId))
        {
            return Forbid();
        }

        if (history.Status != ClinicalHistoryStatus.Submitted)
        {
            return BadRequest(new { message = "La historia clínica debe estar en estado Submitted para revisar." });
        }

        history.ReviewedByProfessorId = professorId;
        history.ProfessorComments = request.ReviewNotes;
        history.Status = request.Approved ? ClinicalHistoryStatus.Approved : ClinicalHistoryStatus.Rejected;
        history.Grade = request.Approved ? request.Grade : null;
        history.ReviewedAt = DateTimeHelper.Now();
        history.UpdatedAt = DateTimeHelper.Now();

        await IncrementTemplateUsageAsync(professorId, request.TemplateIds);
        await _db.SaveChangesAsync();

        history.ReviewedByProfessor = await _db.Users.FindAsync(professorId);

        return Ok(MapToDto(history));
    }

    [HttpPost("batch-review")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult<BatchReviewAcademicClinicalHistoriesResponse>> BatchReview(
        [FromBody] BatchReviewAcademicClinicalHistoriesRequest request)
    {
        var professorId = GetUserId();
        var isAdmin = User.IsInRole(Roles.Admin);
        var ids = (request.HistoryIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return BadRequest(new { message = "Debe enviar al menos una historia clínica." });
        }

        if (!TryParseDecision(request.Decision, out var decision))
        {
            return BadRequest(new { message = "La decisión debe ser approve, reject o requestChanges." });
        }

        if (!IsValidGrade(request.Grade))
        {
            return BadRequest(new { message = "La calificación debe estar entre 0 y 10." });
        }

        var now = DateTimeHelper.Now();
        var histories = await _db.AcademicClinicalHistories
            .Where(h => ids.Contains(h.Id))
            .ToDictionaryAsync(h => h.Id);
        var supervisedStudentIds = isAdmin
            ? new HashSet<Guid>()
            : await _scope.GetSupervisedStudentIdsAsync(professorId);

        var updatedIds = new List<Guid>();
        var skipped = new List<BatchReviewSkippedItem>();

        foreach (var id in ids)
        {
            if (!histories.TryGetValue(id, out var history))
            {
                skipped.Add(new BatchReviewSkippedItem(id, "No encontrada."));
                continue;
            }

            if (history.Status != ClinicalHistoryStatus.Submitted)
            {
                skipped.Add(new BatchReviewSkippedItem(id, "La historia no está en estado Submitted."));
                continue;
            }

            if (!isAdmin && !supervisedStudentIds.Contains(history.StudentId))
            {
                skipped.Add(new BatchReviewSkippedItem(id, "Sin asignación activa para este estudiante."));
                continue;
            }

            history.ReviewedByProfessorId = professorId;
            history.ProfessorComments = request.ReviewNotes;
            history.Status = decision == BatchReviewDecision.Approve
                ? ClinicalHistoryStatus.Approved
                : ClinicalHistoryStatus.Rejected;
            history.Grade = decision == BatchReviewDecision.Approve ? request.Grade : null;
            history.ReviewedAt = now;
            history.UpdatedAt = now;

            updatedIds.Add(id);
        }

        await IncrementTemplateUsageAsync(professorId, request.TemplateIds);
        await _db.SaveChangesAsync();

        return Ok(new BatchReviewAcademicClinicalHistoriesResponse(
            ids.Count,
            updatedIds.Count,
            skipped.Count,
            updatedIds,
            skipped));
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult<ProfessorClinicalDashboardDto>> GetDashboard()
    {
        var professorId = GetUserId();
        var isAdmin = User.IsInRole(Roles.Admin);

        var supervisedStudentIds = isAdmin
            ? await _db.AcademicClinicalHistories
                .AsNoTracking()
                .Where(h => h.Status == ClinicalHistoryStatus.Submitted)
                .Select(h => h.StudentId)
                .Distinct()
                .ToListAsync()
            : (await _scope.GetSupervisedStudentIdsAsync(professorId)).ToList();

        if (!isAdmin && supervisedStudentIds.Count == 0)
        {
            return Ok(new ProfessorClinicalDashboardDto(
                0,
                0,
                0,
                0,
                0,
                Array.Empty<ProfessorCommonErrorDto>(),
                Array.Empty<ProfessorErrorByStudentDto>(),
                Array.Empty<ProfessorErrorByGroupDto>(),
                Array.Empty<StudentProgressDto>(),
                Array.Empty<PrioritizedReviewDto>()));
        }

        var supervisedSet = supervisedStudentIds.ToHashSet();
        var histories = await _db.AcademicClinicalHistories
            .Include(h => h.Student)
            .AsNoTracking()
            .Where(h => supervisedSet.Contains(h.StudentId))
            .ToListAsync();

        var pendingReviews = histories.Count(h => h.Status == ClinicalHistoryStatus.Submitted);
        var reviewedByProfessor = histories
            .Where(h => h.ReviewedByProfessorId == professorId && h.ReviewedAt.HasValue)
            .ToList();
        var approvedByProfessor = reviewedByProfessor
            .Where(h => h.Status == ClinicalHistoryStatus.Approved && h.SubmittedAt.HasValue)
            .ToList();

        var averageApprovalHours = approvedByProfessor.Count == 0
            ? 0
            : approvedByProfessor.Average(h =>
            {
                var submittedAt = h.SubmittedAt ?? h.CreatedAt;
                return (h.ReviewedAt!.Value - submittedAt).TotalHours;
            });

        var commonErrors = reviewedByProfessor
            .Where(h => h.Status == ClinicalHistoryStatus.Rejected && !string.IsNullOrWhiteSpace(h.ProfessorComments))
            .GroupBy(h => NormalizeComment(h.ProfessorComments!))
            .Select(group => new ProfessorCommonErrorDto(
                group.First().ProfessorComments!.Trim(),
                group.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Comment)
            .Take(10)
            .ToList();

        var errorsByStudent = reviewedByProfessor
            .Where(h => h.Status == ClinicalHistoryStatus.Rejected)
            .GroupBy(h => h.Student?.FullName ?? "Alumno")
            .Select(group => new ProfessorErrorByStudentDto(group.Key, group.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.StudentName)
            .Take(10)
            .ToList();

        var errorsByGroup = reviewedByProfessor
            .Where(h => h.Status == ClinicalHistoryStatus.Rejected)
            .GroupBy(h => string.IsNullOrWhiteSpace(h.Student?.UniversityId) ? "Sin grupo" : h.Student!.UniversityId!)
            .Select(group => new ProfessorErrorByGroupDto(group.Key, group.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.GroupName)
            .Take(10)
            .ToList();

        var students = await _db.Users
            .AsNoTracking()
            .Where(u => supervisedSet.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        var progress = histories
            .GroupBy(h => h.StudentId)
            .Select(group =>
            {
                students.TryGetValue(group.Key, out var student);
                var total = group.Count();
                var approved = group.Count(h => h.Status == ClinicalHistoryStatus.Approved);
                var pending = group.Count(h => h.Status == ClinicalHistoryStatus.Submitted);
                var rejected = group.Count(h => h.Status == ClinicalHistoryStatus.Rejected);
                var grades = group.Where(h => h.Grade.HasValue).Select(h => h.Grade!.Value).ToList();
                var averageGrade = grades.Count == 0 ? (decimal?)null : Math.Round(grades.Average(), 2);
                var progressPercent = total == 0 ? 0 : Math.Round((decimal)approved * 100m / total, 2);

                return new StudentProgressDto(
                    group.Key,
                    student?.FullName ?? "Alumno",
                    string.IsNullOrWhiteSpace(student?.UniversityId) ? null : student!.UniversityId,
                    total,
                    pending,
                    approved,
                    rejected,
                    averageGrade,
                    progressPercent);
            })
            .OrderByDescending(x => x.ProgressPercent)
            .ThenBy(x => x.StudentName)
            .ToList();

        var unresolvedRiskLevels = await _db.AcademicStudentRiskFlags
            .AsNoTracking()
            .Where(f => !f.IsResolved && supervisedSet.Contains(f.StudentId))
            .GroupBy(f => f.StudentId)
            .Select(group => new
            {
                StudentId = group.Key,
                MaxRisk = group.Max(f => f.RiskLevel)
            })
            .ToDictionaryAsync(x => x.StudentId, x => x.MaxRisk);

        var prioritizedReviews = histories
            .Where(h => h.Status == ClinicalHistoryStatus.Submitted)
            .Select(h =>
            {
                var submittedAt = h.SubmittedAt ?? h.UpdatedAt;
                var waitingHours = Math.Max(0, (DateTimeHelper.Now() - submittedAt).TotalHours);
                var recentRejectedCount = histories.Count(item =>
                    item.StudentId == h.StudentId &&
                    item.Status == ClinicalHistoryStatus.Rejected &&
                    item.UpdatedAt >= DateTimeHelper.Now().AddDays(-60));
                var hasRiskFlag = unresolvedRiskLevels.TryGetValue(h.StudentId, out var riskLevel);

                var riskWeight = !hasRiskFlag ? 0m : riskLevel switch
                {
                    StudentRiskLevel.Critical => 6m,
                    StudentRiskLevel.High => 4m,
                    StudentRiskLevel.Medium => 2m,
                    StudentRiskLevel.Low => 0.5m,
                    _ => 0m
                };

                var priorityScore = Math.Round(((decimal)waitingHours * 0.20m) + (recentRejectedCount * 2m) + riskWeight, 2);
                var slaStatus = waitingHours switch
                {
                    >= 72 => "Critico",
                    >= 48 => "EnRiesgo",
                    _ => "Normal"
                };

                return new PrioritizedReviewDto(
                    h.Id,
                    h.StudentId,
                    h.Student?.FullName ?? "Alumno",
                    h.PatientNameFromData(),
                    submittedAt,
                    Math.Round(waitingHours, 2),
                    recentRejectedCount,
                    hasRiskFlag ? riskLevel.ToString() : "SinBandera",
                    slaStatus,
                    priorityScore);
            })
            .OrderByDescending(item => item.PriorityScore)
            .ThenByDescending(item => item.HoursWaiting)
            .Take(20)
            .ToList();

        return Ok(new ProfessorClinicalDashboardDto(
            pendingReviews,
            reviewedByProfessor.Count,
            approvedByProfessor.Count,
            reviewedByProfessor.Count(h => h.Status == ClinicalHistoryStatus.Rejected),
            Math.Round(averageApprovalHours, 2),
            commonErrors,
            errorsByStudent,
            errorsByGroup,
            progress,
            prioritizedReviews));
    }

    [HttpGet("comment-templates")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult<IEnumerable<AcademicCommentTemplateDto>>> GetCommentTemplates()
    {
        var professorId = GetUserId();
        var templates = await _db.AcademicReviewCommentTemplates
            .AsNoTracking()
            .Where(t => t.ProfessorId == professorId)
            .OrderByDescending(t => t.UsageCount)
            .ThenBy(t => t.Title)
            .ToListAsync();

        return Ok(templates.Select(MapTemplateToDto));
    }

    [HttpPost("comment-templates")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult<AcademicCommentTemplateDto>> CreateCommentTemplate(
        [FromBody] UpsertAcademicCommentTemplateRequest request)
    {
        var professorId = GetUserId();
        var title = request.Title?.Trim() ?? string.Empty;
        var commentText = request.CommentText?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(commentText))
        {
            return BadRequest(new { message = "Título y comentario son obligatorios." });
        }

        var exists = await _db.AcademicReviewCommentTemplates.AnyAsync(t =>
            t.ProfessorId == professorId && t.Title == title);
        if (exists)
        {
            return Conflict(new { message = "Ya existe una plantilla con ese título." });
        }

        var template = new AcademicReviewCommentTemplate
        {
            Id = Guid.NewGuid(),
            ProfessorId = professorId,
            Title = title,
            CommentText = commentText,
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            UsageCount = 0,
            CreatedAt = DateTimeHelper.Now(),
            UpdatedAt = DateTimeHelper.Now()
        };

        _db.AcademicReviewCommentTemplates.Add(template);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCommentTemplates), MapTemplateToDto(template));
    }

    [HttpPut("comment-templates/{id:guid}")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult<AcademicCommentTemplateDto>> UpdateCommentTemplate(
        Guid id,
        [FromBody] UpsertAcademicCommentTemplateRequest request)
    {
        var professorId = GetUserId();
        var title = request.Title?.Trim() ?? string.Empty;
        var commentText = request.CommentText?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(commentText))
        {
            return BadRequest(new { message = "Título y comentario son obligatorios." });
        }

        var template = await _db.AcademicReviewCommentTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.ProfessorId == professorId);
        if (template == null)
        {
            return NotFound();
        }

        var titleExists = await _db.AcademicReviewCommentTemplates.AnyAsync(t =>
            t.ProfessorId == professorId && t.Id != id && t.Title == title);
        if (titleExists)
        {
            return Conflict(new { message = "Ya existe una plantilla con ese título." });
        }

        template.Title = title;
        template.CommentText = commentText;
        template.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        template.UpdatedAt = DateTimeHelper.Now();

        await _db.SaveChangesAsync();

        return Ok(MapTemplateToDto(template));
    }

    [HttpDelete("comment-templates/{id:guid}")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult> DeleteCommentTemplate(Guid id)
    {
        var professorId = GetUserId();
        var template = await _db.AcademicReviewCommentTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.ProfessorId == professorId);
        if (template == null)
        {
            return NotFound();
        }

        _db.AcademicReviewCommentTemplates.Remove(template);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("comment-templates/{id:guid}/use")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult> MarkCommentTemplateAsUsed(Guid id)
    {
        var professorId = GetUserId();
        var template = await _db.AcademicReviewCommentTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.ProfessorId == professorId);
        if (template == null)
        {
            return NotFound();
        }

        template.UsageCount += 1;
        template.UpdatedAt = DateTimeHelper.Now();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task IncrementTemplateUsageAsync(Guid professorId, IEnumerable<Guid>? templateIds)
    {
        if (templateIds == null)
        {
            return;
        }

        var ids = templateIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var templates = await _db.AcademicReviewCommentTemplates
            .Where(t => t.ProfessorId == professorId && ids.Contains(t.Id))
            .ToListAsync();

        foreach (var template in templates)
        {
            template.UsageCount += 1;
            template.UpdatedAt = DateTimeHelper.Now();
        }
    }

    private static AcademicClinicalHistoryDto MapToDto(AcademicClinicalHistory history)
    {
        var patientFirstName = GetDataValue(history.Data, "personal", "firstName");
        var patientLastName = GetDataValue(history.Data, "personal", "lastName");
        var patientIdNumber = GetDataValue(history.Data, "personal", "idNumber");
        var patientName = $"{patientFirstName} {patientLastName}".Trim();

        return new AcademicClinicalHistoryDto(
            history.Id,
            history.StudentId,
            history.Student?.FullName ?? string.Empty,
            string.IsNullOrWhiteSpace(patientName) ? "Paciente no registrado" : patientName,
            patientIdNumber,
            history.Data,
            history.Status.ToString(),
            history.ReviewedByProfessorId,
            history.ReviewedByProfessor?.FullName,
            history.ProfessorComments,
            history.Grade,
            history.SubmittedAt,
            history.ReviewedAt,
            history.CreatedAt,
            history.UpdatedAt
        );
    }

    private static string GetDataValue(JsonObject? data, params string[] path)
    {
        JsonNode? current = data;
        foreach (var segment in path)
        {
            if (current is not JsonObject currentObject || !currentObject.TryGetPropertyValue(segment, out current))
            {
                return string.Empty;
            }
        }

        var value = current?.ToString() ?? string.Empty;
        return value.Trim();
    }

    private static string NormalizeComment(string value)
    {
        var chunks = value
            .Trim()
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(' ', chunks);
    }

    private static bool IsValidGrade(decimal? grade)
        => !grade.HasValue || (grade.Value >= 0m && grade.Value <= 10m);

    private static bool TryParseDecision(string? decisionText, out BatchReviewDecision decision)
    {
        decision = BatchReviewDecision.Reject;
        var normalized = decisionText?.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "approve":
            case "approved":
            case "aprobar":
                decision = BatchReviewDecision.Approve;
                return true;
            case "reject":
            case "rejected":
            case "rechazar":
                decision = BatchReviewDecision.Reject;
                return true;
            case "requestchanges":
            case "request-changes":
            case "changesrequested":
            case "solicitar-cambios":
            case "solicitarcambios":
                decision = BatchReviewDecision.RequestChanges;
                return true;
            default:
                return false;
        }
    }

    private static AcademicCommentTemplateDto MapTemplateToDto(AcademicReviewCommentTemplate template) => new(
        template.Id,
        template.Title,
        template.CommentText,
        template.Category,
        template.UsageCount,
        template.CreatedAt,
        template.UpdatedAt);
}

public record CreateAcademicClinicalHistoryRequest(JsonObject? Data);

public record UpdateAcademicClinicalHistoryRequest(JsonObject? Data);

public record ReviewAcademicClinicalHistoryRequest(
    bool Approved,
    [property: JsonPropertyName("reviewNotes")] string? ReviewNotes,
    [property: JsonPropertyName("grade")] decimal? Grade,
    [property: JsonPropertyName("templateIds")] IReadOnlyCollection<Guid>? TemplateIds);

public record BatchReviewAcademicClinicalHistoriesRequest(
    [property: JsonPropertyName("historyIds")] IReadOnlyCollection<Guid> HistoryIds,
    [property: JsonPropertyName("decision")] string Decision,
    [property: JsonPropertyName("reviewNotes")] string? ReviewNotes,
    [property: JsonPropertyName("grade")] decimal? Grade,
    [property: JsonPropertyName("templateIds")] IReadOnlyCollection<Guid>? TemplateIds);

public record BatchReviewSkippedItem(Guid HistoryId, string Reason);

public record BatchReviewAcademicClinicalHistoriesResponse(
    int Requested,
    int Updated,
    int Skipped,
    IReadOnlyCollection<Guid> UpdatedIds,
    IReadOnlyCollection<BatchReviewSkippedItem> SkippedItems);

public record AcademicClinicalHistoryDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string PatientName,
    string PatientIdNumber,
    JsonObject Data,
    string Status,
    Guid? ReviewedByProfessorId,
    string? ReviewedByProfessorName,
    string? ProfessorComments,
    decimal? Grade,
    DateTime? SubmittedAt,
    DateTime? ReviewedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ProfessorClinicalDashboardDto(
    int PendingReviews,
    int TotalReviewed,
    int ApprovedCount,
    int RejectedCount,
    double AverageApprovalHours,
    IReadOnlyCollection<ProfessorCommonErrorDto> CommonErrors,
    IReadOnlyCollection<ProfessorErrorByStudentDto> ErrorsByStudent,
    IReadOnlyCollection<ProfessorErrorByGroupDto> ErrorsByGroup,
    IReadOnlyCollection<StudentProgressDto> StudentProgress,
    IReadOnlyCollection<PrioritizedReviewDto> PrioritizedReviews
);

public record ProfessorCommonErrorDto(string Comment, int Count);

public record ProfessorErrorByStudentDto(string StudentName, int Count);

public record ProfessorErrorByGroupDto(string GroupName, int Count);

public record StudentProgressDto(
    Guid StudentId,
    string StudentName,
    string? GroupName,
    int TotalHistories,
    int PendingReviews,
    int ApprovedHistories,
    int RejectedHistories,
    decimal? AverageGrade,
    decimal ProgressPercent);

public record PrioritizedReviewDto(
    Guid HistoryId,
    Guid StudentId,
    string StudentName,
    string PatientName,
    DateTime SubmittedAt,
    double HoursWaiting,
    int RecentRejectedCount,
    string RiskLevel,
    string SlaStatus,
    decimal PriorityScore);

public record UpsertAcademicCommentTemplateRequest(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("commentText")] string? CommentText,
    [property: JsonPropertyName("category")] string? Category);

public record AcademicCommentTemplateDto(
    Guid Id,
    string Title,
    string CommentText,
    string? Category,
    int UsageCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public enum BatchReviewDecision
{
    Approve,
    Reject,
    RequestChanges
}

internal static class AcademicClinicalHistoryExtensions
{
    public static string PatientNameFromData(this AcademicClinicalHistory history)
    {
        var firstName = GetDataValue(history.Data, "personal", "firstName");
        var lastName = GetDataValue(history.Data, "personal", "lastName");
        var fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? "Paciente no registrado" : fullName;
    }

    private static string GetDataValue(JsonObject? data, params string[] path)
    {
        JsonNode? current = data;
        foreach (var segment in path)
        {
            if (current is not JsonObject currentObject || !currentObject.TryGetPropertyValue(segment, out current))
            {
                return string.Empty;
            }
        }

        return (current?.ToString() ?? string.Empty).Trim();
    }
}
