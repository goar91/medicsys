using System.Security.Claims;
using System.Text.Json.Nodes;
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
[Route("api/academic/compliance")]
[Authorize]
public class AcademicComplianceController : ControllerBase
{
    private readonly AcademicDbContext _db;

    public AcademicComplianceController(AcademicDbContext db)
    {
        _db = db;
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult<ComplianceDashboardDto>> GetDashboard()
    {
        var now = DateTimeHelper.Now();
        var consents = await _db.AcademicDataConsents.AsNoTracking().ToListAsync();
        var anonymizationRequests = await _db.AcademicDataAnonymizationRequests.AsNoTracking().ToListAsync();
        var retentionPolicies = await _db.AcademicDataRetentionPolicies.AsNoTracking().ToListAsync();
        var recentAuditEvents = await _db.AcademicDataAuditEvents
            .AsNoTracking()
            .OrderByDescending(e => e.OccurredAt)
            .Take(20)
            .ToListAsync();

        var grantedConsents = consents.Count(c => c.Granted && !c.RevokedAt.HasValue);
        var revokedConsents = consents.Count(c => c.RevokedAt.HasValue);
        var pendingAnonymization = anonymizationRequests.Count(r => r.Status == DataAnonymizationRequestStatus.Pending);
        var approvedAnonymization = anonymizationRequests.Count(r => r.Status == DataAnonymizationRequestStatus.Approved);
        var completedAnonymization = anonymizationRequests.Count(r => r.Status == DataAnonymizationRequestStatus.Completed);
        var activePolicies = retentionPolicies.Count(p => p.IsActive);

        var expiringSubjects = consents
            .Where(c => c.Granted && !c.RevokedAt.HasValue && c.GrantedAt <= now.AddYears(-2))
            .GroupBy(c => c.SubjectIdentifier)
            .Select(g => g.Key)
            .Count();

        return Ok(new ComplianceDashboardDto(
            consents.Count,
            grantedConsents,
            revokedConsents,
            pendingAnonymization,
            approvedAnonymization,
            completedAnonymization,
            activePolicies,
            expiringSubjects,
            recentAuditEvents.Select(e => new ComplianceAuditEventDto(
                e.Id,
                e.EventType.ToString(),
                e.Path,
                e.Method,
                e.StatusCode,
                e.UserEmail,
                e.UserRole,
                e.SubjectType,
                e.SubjectIdentifier,
                e.OccurredAt)).ToList()));
    }

    [HttpGet("consents")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult<IEnumerable<ComplianceConsentDto>>> GetConsents([FromQuery] string? subjectType, [FromQuery] string? subjectIdentifier)
    {
        var query = _db.AcademicDataConsents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(subjectType))
        {
            query = query.Where(c => c.SubjectType == subjectType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(subjectIdentifier))
        {
            query = query.Where(c => c.SubjectIdentifier == subjectIdentifier.Trim());
        }

        var consents = await query
            .OrderByDescending(c => c.GrantedAt)
            .Take(200)
            .ToListAsync();

        return Ok(consents.Select(MapConsent));
    }

    [HttpPost("consents")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult<ComplianceConsentDto>> CreateConsent([FromBody] CreateComplianceConsentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SubjectType) ||
            string.IsNullOrWhiteSpace(request.SubjectIdentifier) ||
            string.IsNullOrWhiteSpace(request.Purpose) ||
            string.IsNullOrWhiteSpace(request.LegalBasis))
        {
            return BadRequest(new { message = "SubjectType, SubjectIdentifier, Purpose y LegalBasis son obligatorios." });
        }

        var now = DateTimeHelper.Now();
        var consent = new AcademicDataConsent
        {
            Id = Guid.NewGuid(),
            SubjectType = request.SubjectType.Trim(),
            SubjectId = request.SubjectId,
            SubjectIdentifier = request.SubjectIdentifier.Trim(),
            Purpose = request.Purpose.Trim(),
            LegalBasis = request.LegalBasis.Trim(),
            Granted = request.Granted,
            GrantedAt = request.Granted ? now : request.GrantedAt ?? now,
            RevokedAt = request.Granted ? null : now,
            CollectedByUserId = GetUserId(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.AcademicDataConsents.Add(consent);
        await _db.SaveChangesAsync();

        return Ok(MapConsent(consent));
    }

    [HttpPut("consents/{id:guid}/revoke")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult<ComplianceConsentDto>> RevokeConsent(Guid id, [FromBody] RevokeConsentRequest request)
    {
        var consent = await _db.AcademicDataConsents.FirstOrDefaultAsync(c => c.Id == id);
        if (consent == null)
        {
            return NotFound();
        }

        consent.Granted = false;
        consent.RevokedAt = DateTimeHelper.Now();
        consent.Notes = string.IsNullOrWhiteSpace(request.Reason)
            ? consent.Notes
            : $"{consent.Notes}\nRevocado: {request.Reason.Trim()}".Trim();
        consent.UpdatedAt = DateTimeHelper.Now();

        await _db.SaveChangesAsync();
        return Ok(MapConsent(consent));
    }

    [HttpGet("anonymization-requests")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult<IEnumerable<AnonymizationRequestDto>>> GetAnonymizationRequests([FromQuery] string? status)
    {
        var query = _db.AcademicDataAnonymizationRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DataAnonymizationRequestStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        var items = await query
            .OrderByDescending(r => r.RequestedAt)
            .Take(300)
            .ToListAsync();

        return Ok(items.Select(MapAnonymizationRequest));
    }

    [HttpPost("anonymization-requests")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult<AnonymizationRequestDto>> CreateAnonymizationRequest([FromBody] CreateAnonymizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SubjectType) ||
            string.IsNullOrWhiteSpace(request.SubjectIdentifier) ||
            string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "SubjectType, SubjectIdentifier y Reason son obligatorios." });
        }

        var item = new AcademicDataAnonymizationRequest
        {
            Id = Guid.NewGuid(),
            SubjectType = request.SubjectType.Trim(),
            SubjectId = request.SubjectId,
            SubjectIdentifier = request.SubjectIdentifier.Trim(),
            Reason = request.Reason.Trim(),
            Status = DataAnonymizationRequestStatus.Pending,
            RequestedByUserId = GetUserId(),
            RequestedAt = DateTimeHelper.Now()
        };

        _db.AcademicDataAnonymizationRequests.Add(item);
        await _db.SaveChangesAsync();

        return Ok(MapAnonymizationRequest(item));
    }

    [HttpPut("anonymization-requests/{id:guid}/review")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<AnonymizationRequestDto>> ReviewAnonymizationRequest(Guid id, [FromBody] ReviewAnonymizationRequest request)
    {
        var item = await _db.AcademicDataAnonymizationRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (item == null)
        {
            return NotFound();
        }

        if (item.Status != DataAnonymizationRequestStatus.Pending)
        {
            return BadRequest(new { message = "La solicitud ya fue revisada." });
        }

        item.Status = request.Approved
            ? DataAnonymizationRequestStatus.Approved
            : DataAnonymizationRequestStatus.Rejected;
        item.ReviewedByUserId = GetUserId();
        item.ReviewedAt = DateTimeHelper.Now();
        item.ResolutionNotes = request.Notes?.Trim();

        await _db.SaveChangesAsync();

        return Ok(MapAnonymizationRequest(item));
    }

    [HttpPut("anonymization-requests/{id:guid}/complete")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<AnonymizationRequestDto>> CompleteAnonymizationRequest(Guid id, [FromBody] CompleteAnonymizationRequest request)
    {
        var item = await _db.AcademicDataAnonymizationRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (item == null)
        {
            return NotFound();
        }

        if (item.Status != DataAnonymizationRequestStatus.Approved)
        {
            return BadRequest(new { message = "Solo se pueden completar solicitudes aprobadas." });
        }

        var changes = await AnonymizeSubjectAsync(item.SubjectType, item.SubjectId, item.SubjectIdentifier, request.MaskPrefix);
        item.Status = DataAnonymizationRequestStatus.Completed;
        item.CompletedAt = DateTimeHelper.Now();
        item.ResolutionNotes = $"{item.ResolutionNotes}\nCompletado con {changes} cambio(s).".Trim();

        await _db.SaveChangesAsync();

        return Ok(MapAnonymizationRequest(item));
    }

    [HttpGet("retention-policies")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IEnumerable<RetentionPolicyDto>>> GetRetentionPolicies()
    {
        var items = await _db.AcademicDataRetentionPolicies
            .AsNoTracking()
            .OrderBy(p => p.DataCategory)
            .ToListAsync();

        return Ok(items.Select(MapRetentionPolicy));
    }

    [HttpPost("retention-policies")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<RetentionPolicyDto>> CreateRetentionPolicy([FromBody] UpsertRetentionPolicyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DataCategory) || request.RetentionMonths <= 0)
        {
            return BadRequest(new { message = "DataCategory y RetentionMonths válidos son obligatorios." });
        }

        var category = request.DataCategory.Trim();
        var exists = await _db.AcademicDataRetentionPolicies.AnyAsync(p => p.DataCategory == category);
        if (exists)
        {
            return Conflict(new { message = "Ya existe una política para esa categoría." });
        }

        var now = DateTimeHelper.Now();
        var item = new AcademicDataRetentionPolicy
        {
            Id = Guid.NewGuid(),
            DataCategory = category,
            RetentionMonths = request.RetentionMonths,
            AutoDelete = request.AutoDelete,
            IsActive = request.IsActive,
            ConfiguredByUserId = GetUserId(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.AcademicDataRetentionPolicies.Add(item);
        await _db.SaveChangesAsync();

        return Ok(MapRetentionPolicy(item));
    }

    [HttpPut("retention-policies/{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<RetentionPolicyDto>> UpdateRetentionPolicy(Guid id, [FromBody] UpsertRetentionPolicyRequest request)
    {
        var item = await _db.AcademicDataRetentionPolicies.FirstOrDefaultAsync(p => p.Id == id);
        if (item == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.DataCategory))
        {
            item.DataCategory = request.DataCategory.Trim();
        }

        if (request.RetentionMonths > 0)
        {
            item.RetentionMonths = request.RetentionMonths;
        }

        item.AutoDelete = request.AutoDelete;
        item.IsActive = request.IsActive;
        item.ConfiguredByUserId = GetUserId();
        item.UpdatedAt = DateTimeHelper.Now();

        await _db.SaveChangesAsync();

        return Ok(MapRetentionPolicy(item));
    }

    [HttpGet("audit-events")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IEnumerable<ComplianceAuditEventDto>>> GetAuditEvents([FromQuery] int take = 200)
    {
        var normalizedTake = Math.Clamp(take, 1, 1000);
        var events = await _db.AcademicDataAuditEvents
            .AsNoTracking()
            .OrderByDescending(e => e.OccurredAt)
            .Take(normalizedTake)
            .ToListAsync();

        return Ok(events.Select(e => new ComplianceAuditEventDto(
            e.Id,
            e.EventType.ToString(),
            e.Path,
            e.Method,
            e.StatusCode,
            e.UserEmail,
            e.UserRole,
            e.SubjectType,
            e.SubjectIdentifier,
            e.OccurredAt)));
    }

    [HttpPost("audit-events")]
    [Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
    public async Task<ActionResult<ComplianceAuditEventDto>> CreateManualAuditEvent([FromBody] CreateManualAuditEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Path) || string.IsNullOrWhiteSpace(request.Method))
        {
            return BadRequest(new { message = "Path y Method son obligatorios." });
        }

        var userId = GetUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        var auditEvent = new AcademicDataAuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = request.EventType ?? DataAuditEventType.Other,
            Path = request.Path.Trim(),
            Method = request.Method.Trim().ToUpperInvariant(),
            StatusCode = request.StatusCode <= 0 ? 200 : request.StatusCode,
            UserId = userId,
            UserEmail = user?.Email,
            UserRole = User.FindFirstValue(ClaimTypes.Role),
            SubjectType = request.SubjectType?.Trim(),
            SubjectIdentifier = request.SubjectIdentifier?.Trim(),
            Reason = request.Reason?.Trim(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            OccurredAt = DateTimeHelper.Now()
        };

        _db.AcademicDataAuditEvents.Add(auditEvent);
        await _db.SaveChangesAsync();

        return Ok(new ComplianceAuditEventDto(
            auditEvent.Id,
            auditEvent.EventType.ToString(),
            auditEvent.Path,
            auditEvent.Method,
            auditEvent.StatusCode,
            auditEvent.UserEmail,
            auditEvent.UserRole,
            auditEvent.SubjectType,
            auditEvent.SubjectIdentifier,
            auditEvent.OccurredAt));
    }

    private async Task<int> AnonymizeSubjectAsync(string subjectType, Guid? subjectId, string subjectIdentifier, string? maskPrefix)
    {
        var normalizedType = subjectType.Trim().ToLowerInvariant();
        var prefix = string.IsNullOrWhiteSpace(maskPrefix) ? "ANON" : maskPrefix.Trim().ToUpperInvariant();
        var nowToken = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var updates = 0;

        if (normalizedType is "patient" or "paciente")
        {
            IQueryable<Models.Academico.AcademicPatient> patientQuery = _db.AcademicPatients;
            if (subjectId.HasValue)
            {
                patientQuery = patientQuery.Where(p => p.Id == subjectId.Value);
            }
            else
            {
                var identifier = subjectIdentifier.Trim();
                patientQuery = patientQuery.Where(p => p.IdNumber == identifier || p.Email == identifier);
            }

            var patients = await patientQuery.ToListAsync();
            foreach (var patient in patients)
            {
                patient.FirstName = prefix;
                patient.LastName = $"{prefix}_{nowToken}";
                patient.IdNumber = $"{prefix}_{patient.Id:N}".Substring(0, Math.Min(20, $"{prefix}_{patient.Id:N}".Length));
                patient.Phone = null;
                patient.Email = null;
                patient.Address = null;
                patient.Allergies = null;
                patient.MedicalConditions = null;
                patient.EmergencyContact = null;
                patient.EmergencyPhone = null;
                patient.UpdatedAt = DateTimeHelper.Now();
                updates++;
            }
        }

        if (normalizedType is "student" or "alumno" or "professor" or "profesor")
        {
            IQueryable<ApplicationUser> users = _db.Users;
            if (subjectId.HasValue)
            {
                users = users.Where(u => u.Id == subjectId.Value);
            }
            else
            {
                var identifier = subjectIdentifier.Trim();
                users = users.Where(u => u.Email == identifier || u.UserName == identifier || u.UniversityId == identifier);
            }

            var targets = await users.ToListAsync();
            foreach (var user in targets)
            {
                user.FullName = $"{prefix}_{user.Id.ToString("N")[..8]}";
                user.PhoneNumber = null;
                user.UniversityId = null;
                updates++;
            }
        }

        return updates;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static ComplianceConsentDto MapConsent(AcademicDataConsent item) => new(
        item.Id,
        item.SubjectType,
        item.SubjectId,
        item.SubjectIdentifier,
        item.Purpose,
        item.LegalBasis,
        item.Granted,
        item.GrantedAt,
        item.RevokedAt,
        item.CollectedByUserId,
        item.Notes,
        item.CreatedAt,
        item.UpdatedAt);

    private static AnonymizationRequestDto MapAnonymizationRequest(AcademicDataAnonymizationRequest item) => new(
        item.Id,
        item.SubjectType,
        item.SubjectId,
        item.SubjectIdentifier,
        item.Reason,
        item.Status.ToString(),
        item.RequestedByUserId,
        item.ReviewedByUserId,
        item.RequestedAt,
        item.ReviewedAt,
        item.CompletedAt,
        item.ResolutionNotes);

    private static RetentionPolicyDto MapRetentionPolicy(AcademicDataRetentionPolicy item) => new(
        item.Id,
        item.DataCategory,
        item.RetentionMonths,
        item.AutoDelete,
        item.IsActive,
        item.ConfiguredByUserId,
        item.CreatedAt,
        item.UpdatedAt);
}

public record ComplianceDashboardDto(
    int TotalConsents,
    int GrantedConsents,
    int RevokedConsents,
    int PendingAnonymizationRequests,
    int ApprovedAnonymizationRequests,
    int CompletedAnonymizationRequests,
    int ActiveRetentionPolicies,
    int SubjectsWithConsentOlderThan2Years,
    IReadOnlyCollection<ComplianceAuditEventDto> RecentAuditEvents);

public record ComplianceConsentDto(
    Guid Id,
    string SubjectType,
    Guid? SubjectId,
    string SubjectIdentifier,
    string Purpose,
    string LegalBasis,
    bool Granted,
    DateTime GrantedAt,
    DateTime? RevokedAt,
    Guid CollectedByUserId,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record AnonymizationRequestDto(
    Guid Id,
    string SubjectType,
    Guid? SubjectId,
    string SubjectIdentifier,
    string Reason,
    string Status,
    Guid RequestedByUserId,
    Guid? ReviewedByUserId,
    DateTime RequestedAt,
    DateTime? ReviewedAt,
    DateTime? CompletedAt,
    string? ResolutionNotes);

public record RetentionPolicyDto(
    Guid Id,
    string DataCategory,
    int RetentionMonths,
    bool AutoDelete,
    bool IsActive,
    Guid ConfiguredByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ComplianceAuditEventDto(
    Guid Id,
    string EventType,
    string Path,
    string Method,
    int StatusCode,
    string? UserEmail,
    string? UserRole,
    string? SubjectType,
    string? SubjectIdentifier,
    DateTime OccurredAt);

public record CreateComplianceConsentRequest(
    string? SubjectType,
    Guid? SubjectId,
    string? SubjectIdentifier,
    string? Purpose,
    string? LegalBasis,
    bool Granted,
    DateTime? GrantedAt,
    string? Notes);

public record RevokeConsentRequest(string? Reason);

public record CreateAnonymizationRequest(
    string? SubjectType,
    Guid? SubjectId,
    string? SubjectIdentifier,
    string? Reason);

public record ReviewAnonymizationRequest(bool Approved, string? Notes);

public record CompleteAnonymizationRequest(string? MaskPrefix);

public record UpsertRetentionPolicyRequest(
    string? DataCategory,
    int RetentionMonths,
    bool AutoDelete,
    bool IsActive);

public record CreateManualAuditEventRequest(
    DataAuditEventType? EventType,
    string? Path,
    string? Method,
    int StatusCode,
    string? SubjectType,
    string? SubjectIdentifier,
    string? Reason,
    JsonObject? Metadata);
