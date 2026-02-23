using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models.Academico;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Controllers.Academico;

[ApiController]
[Route("api/academic/caces")]
[Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
public class AcademicCacesController : ControllerBase
{
    private readonly AcademicDbContext _db;

    public AcademicCacesController(AcademicDbContext db)
    {
        _db = db;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<CacesDashboardDto>> GetDashboard()
    {
        var now = DateTimeHelper.Now();
        var criteria = await _db.AcademicAccreditationCriteria
            .Include(c => c.Evidences)
            .Include(c => c.ImprovementActions)
            .AsNoTracking()
            .ToListAsync();

        var total = criteria.Count;
        var compliant = criteria.Count(c => c.Status == AccreditationCriterionStatus.Compliant);
        var inProgress = criteria.Count(c => c.Status == AccreditationCriterionStatus.InProgress);
        var needsImprovement = criteria.Count(c => c.Status == AccreditationCriterionStatus.NeedsImprovement);

        var overdueActions = criteria
            .SelectMany(c => c.ImprovementActions)
            .Count(a => a.Status != ImprovementActionStatus.Completed && a.DueDate < now);

        var pendingEvidence = criteria
            .SelectMany(c => c.Evidences)
            .Count(e => !e.IsVerified);

        var atRisk = criteria
            .Where(c =>
                c.Status == AccreditationCriterionStatus.NeedsImprovement ||
                c.CurrentValue < c.TargetValue)
            .OrderBy(c => c.CurrentValue - c.TargetValue)
            .Take(10)
            .Select(c => new CacesAtRiskCriterionDto(
                c.Id,
                c.Code,
                c.Name,
                c.Dimension,
                c.TargetValue,
                c.CurrentValue,
                c.Status.ToString()))
            .ToList();

        var complianceRate = total == 0 ? 0m : Math.Round((decimal)compliant * 100m / total, 2);

        return Ok(new CacesDashboardDto(
            total,
            compliant,
            inProgress,
            needsImprovement,
            complianceRate,
            overdueActions,
            pendingEvidence,
            atRisk));
    }

    [HttpGet("criteria")]
    public async Task<ActionResult<IEnumerable<CacesCriterionDto>>> GetCriteria([FromQuery] string? dimension)
    {
        var query = _db.AcademicAccreditationCriteria
            .Include(c => c.Evidences)
            .Include(c => c.ImprovementActions)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(dimension))
        {
            var normalized = dimension.Trim();
            query = query.Where(c => c.Dimension == normalized);
        }

        var criteria = await query
            .OrderBy(c => c.Dimension)
            .ThenBy(c => c.Code)
            .ToListAsync();

        return Ok(criteria.Select(c => new CacesCriterionDto(
            c.Id,
            c.Code,
            c.Name,
            c.Dimension,
            c.Description,
            c.TargetValue,
            c.CurrentValue,
            c.Status.ToString(),
            c.Evidences.Count,
            c.Evidences.Count(e => e.IsVerified),
            c.ImprovementActions.Count,
            c.ImprovementActions.Count(a => a.Status == ImprovementActionStatus.Completed),
            c.CreatedAt,
            c.UpdatedAt)));
    }

    [HttpPost("criteria")]
    public async Task<ActionResult<CacesCriterionDto>> CreateCriterion([FromBody] UpsertCacesCriterionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Dimension))
        {
            return BadRequest(new { message = "Code, Name y Dimension son obligatorios." });
        }

        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await _db.AcademicAccreditationCriteria.AnyAsync(c => c.Code == code);
        if (exists)
        {
            return Conflict(new { message = "Ya existe un criterio con ese código." });
        }

        var now = DateTimeHelper.Now();
        var criterion = new AcademicAccreditationCriterion
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            Dimension = request.Dimension.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            TargetValue = request.TargetValue,
            CurrentValue = request.CurrentValue,
            Status = request.Status ?? ResolveStatus(request.CurrentValue, request.TargetValue),
            CreatedByUserId = GetUserId(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.AcademicAccreditationCriteria.Add(criterion);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCriteria), new CacesCriterionDto(
            criterion.Id,
            criterion.Code,
            criterion.Name,
            criterion.Dimension,
            criterion.Description,
            criterion.TargetValue,
            criterion.CurrentValue,
            criterion.Status.ToString(),
            0,
            0,
            0,
            0,
            criterion.CreatedAt,
            criterion.UpdatedAt));
    }

    [HttpPut("criteria/{id:guid}")]
    public async Task<ActionResult<CacesCriterionDto>> UpdateCriterion(Guid id, [FromBody] UpsertCacesCriterionRequest request)
    {
        var criterion = await _db.AcademicAccreditationCriteria
            .Include(c => c.Evidences)
            .Include(c => c.ImprovementActions)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (criterion == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var normalizedCode = request.Code.Trim().ToUpperInvariant();
            var duplicate = await _db.AcademicAccreditationCriteria
                .AnyAsync(c => c.Id != id && c.Code == normalizedCode);
            if (duplicate)
            {
                return Conflict(new { message = "Ya existe otro criterio con ese código." });
            }
            criterion.Code = normalizedCode;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            criterion.Name = request.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Dimension))
        {
            criterion.Dimension = request.Dimension.Trim();
        }

        criterion.Description = request.Description?.Trim();
        criterion.TargetValue = request.TargetValue;
        criterion.CurrentValue = request.CurrentValue;
        criterion.Status = request.Status ?? ResolveStatus(request.CurrentValue, request.TargetValue);
        criterion.UpdatedAt = DateTimeHelper.Now();

        await _db.SaveChangesAsync();

        return Ok(new CacesCriterionDto(
            criterion.Id,
            criterion.Code,
            criterion.Name,
            criterion.Dimension,
            criterion.Description,
            criterion.TargetValue,
            criterion.CurrentValue,
            criterion.Status.ToString(),
            criterion.Evidences.Count,
            criterion.Evidences.Count(e => e.IsVerified),
            criterion.ImprovementActions.Count,
            criterion.ImprovementActions.Count(a => a.Status == ImprovementActionStatus.Completed),
            criterion.CreatedAt,
            criterion.UpdatedAt));
    }

    [HttpPost("criteria/{id:guid}/evidences")]
    public async Task<ActionResult<CacesEvidenceDto>> AddEvidence(Guid id, [FromBody] CreateCacesEvidenceRequest request)
    {
        var criterion = await _db.AcademicAccreditationCriteria.FirstOrDefaultAsync(c => c.Id == id);
        if (criterion == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Title es obligatorio." });
        }

        var evidence = new AcademicAccreditationEvidence
        {
            Id = Guid.NewGuid(),
            CriterionId = id,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "Document" : request.SourceType.Trim(),
            EvidenceUrl = request.EvidenceUrl?.Trim(),
            IsVerified = false,
            UploadedByUserId = GetUserId(),
            CreatedAt = DateTimeHelper.Now()
        };

        _db.AcademicAccreditationEvidences.Add(evidence);
        criterion.UpdatedAt = DateTimeHelper.Now();
        await _db.SaveChangesAsync();

        return Ok(new CacesEvidenceDto(
            evidence.Id,
            evidence.CriterionId,
            evidence.Title,
            evidence.Description,
            evidence.SourceType,
            evidence.EvidenceUrl,
            evidence.IsVerified,
            evidence.VerifiedByUserId,
            evidence.VerifiedAt,
            evidence.CreatedAt));
    }

    [HttpPut("evidences/{id:guid}/verify")]
    public async Task<ActionResult<CacesEvidenceDto>> VerifyEvidence(Guid id, [FromBody] VerifyCacesEvidenceRequest request)
    {
        var evidence = await _db.AcademicAccreditationEvidences.FirstOrDefaultAsync(e => e.Id == id);
        if (evidence == null)
        {
            return NotFound();
        }

        evidence.IsVerified = request.IsVerified;
        evidence.VerifiedByUserId = request.IsVerified ? GetUserId() : null;
        evidence.VerifiedAt = request.IsVerified ? DateTimeHelper.Now() : null;

        var criterion = await _db.AcademicAccreditationCriteria.FirstAsync(c => c.Id == evidence.CriterionId);
        criterion.UpdatedAt = DateTimeHelper.Now();

        await _db.SaveChangesAsync();

        return Ok(new CacesEvidenceDto(
            evidence.Id,
            evidence.CriterionId,
            evidence.Title,
            evidence.Description,
            evidence.SourceType,
            evidence.EvidenceUrl,
            evidence.IsVerified,
            evidence.VerifiedByUserId,
            evidence.VerifiedAt,
            evidence.CreatedAt));
    }

    [HttpPost("criteria/{id:guid}/improvement-actions")]
    public async Task<ActionResult<CacesImprovementActionDto>> AddImprovementAction(Guid id, [FromBody] CreateImprovementActionRequest request)
    {
        var criterion = await _db.AcademicAccreditationCriteria.FirstOrDefaultAsync(c => c.Id == id);
        if (criterion == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Action) || string.IsNullOrWhiteSpace(request.Responsible))
        {
            return BadRequest(new { message = "Action y Responsible son obligatorios." });
        }

        var status = request.Status ?? ImprovementActionStatus.Planned;
        var now = DateTimeHelper.Now();
        if (status != ImprovementActionStatus.Completed && request.DueDate < now)
        {
            status = ImprovementActionStatus.Overdue;
        }

        var action = new AcademicImprovementPlanAction
        {
            Id = Guid.NewGuid(),
            CriterionId = id,
            Action = request.Action.Trim(),
            Responsible = request.Responsible.Trim(),
            DueDate = request.DueDate,
            ProgressPercent = Math.Clamp(request.ProgressPercent, 0m, 100m),
            Status = status,
            CreatedByUserId = GetUserId(),
            CreatedAt = now,
            UpdatedAt = now,
            CompletedAt = status == ImprovementActionStatus.Completed ? now : null
        };

        _db.AcademicImprovementPlanActions.Add(action);
        criterion.UpdatedAt = now;
        await _db.SaveChangesAsync();

        return Ok(MapImprovementAction(action));
    }

    [HttpPut("improvement-actions/{id:guid}")]
    public async Task<ActionResult<CacesImprovementActionDto>> UpdateImprovementAction(Guid id, [FromBody] UpdateImprovementActionRequest request)
    {
        var action = await _db.AcademicImprovementPlanActions.FirstOrDefaultAsync(a => a.Id == id);
        if (action == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            action.Action = request.Action.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Responsible))
        {
            action.Responsible = request.Responsible.Trim();
        }

        if (request.DueDate.HasValue)
        {
            action.DueDate = request.DueDate.Value;
        }

        if (request.ProgressPercent.HasValue)
        {
            action.ProgressPercent = Math.Clamp(request.ProgressPercent.Value, 0m, 100m);
        }

        if (request.Status.HasValue)
        {
            action.Status = request.Status.Value;
        }

        var now = DateTimeHelper.Now();
        if (action.Status == ImprovementActionStatus.Completed)
        {
            action.CompletedAt ??= now;
            action.ProgressPercent = 100m;
        }
        else if (action.DueDate < now && action.Status != ImprovementActionStatus.Overdue)
        {
            action.Status = ImprovementActionStatus.Overdue;
        }

        action.UpdatedAt = now;

        var criterion = await _db.AcademicAccreditationCriteria.FirstAsync(c => c.Id == action.CriterionId);
        criterion.UpdatedAt = now;

        await _db.SaveChangesAsync();

        return Ok(MapImprovementAction(action));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static AccreditationCriterionStatus ResolveStatus(decimal currentValue, decimal targetValue)
    {
        if (targetValue <= 0)
        {
            return AccreditationCriterionStatus.NotStarted;
        }

        if (currentValue <= 0)
        {
            return AccreditationCriterionStatus.NotStarted;
        }

        if (currentValue >= targetValue)
        {
            return AccreditationCriterionStatus.Compliant;
        }

        if (currentValue >= targetValue * 0.7m)
        {
            return AccreditationCriterionStatus.InProgress;
        }

        return AccreditationCriterionStatus.NeedsImprovement;
    }

    private static CacesImprovementActionDto MapImprovementAction(AcademicImprovementPlanAction action) => new(
        action.Id,
        action.CriterionId,
        action.Action,
        action.Responsible,
        action.DueDate,
        action.ProgressPercent,
        action.Status.ToString(),
        action.CompletedAt,
        action.CreatedAt,
        action.UpdatedAt);
}

public record CacesDashboardDto(
    int TotalCriteria,
    int CompliantCriteria,
    int InProgressCriteria,
    int NeedsImprovementCriteria,
    decimal ComplianceRate,
    int OverdueActions,
    int PendingEvidenceVerification,
    IReadOnlyCollection<CacesAtRiskCriterionDto> AtRiskCriteria);

public record CacesAtRiskCriterionDto(
    Guid Id,
    string Code,
    string Name,
    string Dimension,
    decimal TargetValue,
    decimal CurrentValue,
    string Status);

public record CacesCriterionDto(
    Guid Id,
    string Code,
    string Name,
    string Dimension,
    string? Description,
    decimal TargetValue,
    decimal CurrentValue,
    string Status,
    int EvidenceCount,
    int VerifiedEvidenceCount,
    int ImprovementActionsCount,
    int CompletedImprovementActionsCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CacesEvidenceDto(
    Guid Id,
    Guid CriterionId,
    string Title,
    string? Description,
    string SourceType,
    string? EvidenceUrl,
    bool IsVerified,
    Guid? VerifiedByUserId,
    DateTime? VerifiedAt,
    DateTime CreatedAt);

public record CacesImprovementActionDto(
    Guid Id,
    Guid CriterionId,
    string Action,
    string Responsible,
    DateTime DueDate,
    decimal ProgressPercent,
    string Status,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record UpsertCacesCriterionRequest(
    string? Code,
    string? Name,
    string? Dimension,
    string? Description,
    decimal TargetValue,
    decimal CurrentValue,
    AccreditationCriterionStatus? Status);

public record CreateCacesEvidenceRequest(
    string? Title,
    string? Description,
    string? SourceType,
    string? EvidenceUrl);

public record VerifyCacesEvidenceRequest(bool IsVerified);

public record CreateImprovementActionRequest(
    string? Action,
    string? Responsible,
    DateTime DueDate,
    decimal ProgressPercent,
    ImprovementActionStatus? Status);

public record UpdateImprovementActionRequest(
    string? Action,
    string? Responsible,
    DateTime? DueDate,
    decimal? ProgressPercent,
    ImprovementActionStatus? Status);
