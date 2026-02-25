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
[Route("api/academic/analytics")]
[Authorize(Roles = $"{Roles.Professor},{Roles.Admin}")]
public class AcademicAnalyticsController : ControllerBase
{
    private readonly AcademicDbContext _db;
    private readonly AcademicScopeService _scope;

    public AcademicAnalyticsController(AcademicDbContext db, AcademicScopeService scope)
    {
        _db = db;
        _scope = scope;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AcademicAnalyticsDashboardDto>> GetDashboard()
    {
        var isAdmin = User.IsInRole(Roles.Admin);
        var profiles = await BuildRiskProfilesAsync(
            isAdmin ? null : await _scope.GetSupervisedStudentIdsAsync(GetUserId()));
        var historyStats = await _db.AcademicClinicalHistories
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Approved = g.Count(x => x.Status == ClinicalHistoryStatus.Approved),
                Rejected = g.Count(x => x.Status == ClinicalHistoryStatus.Rejected),
                Pending = g.Count(x => x.Status == ClinicalHistoryStatus.Submitted)
            })
            .FirstOrDefaultAsync();

        var appointmentsStats = await _db.AcademicAppointments
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Pending = g.Count(x => x.Status == AppointmentStatus.Pending),
                Approved = g.Count(x => x.Status == AppointmentStatus.Confirmed),
                Rejected = g.Count(x => x.Status == AppointmentStatus.Cancelled)
            })
            .FirstOrDefaultAsync();

        var highRisk = profiles.Count(p => p.RiskLevel is "High" or "Critical");
        var mediumRisk = profiles.Count(p => p.RiskLevel == "Medium");
        var approvalRate = historyStats?.Total > 0
            ? Math.Round((decimal)historyStats.Approved * 100m / historyStats.Total, 2)
            : 0m;

        var byCohort = profiles
            .GroupBy(p => p.Cohort)
            .Select(g => new AcademicCohortMetricDto(
                g.Key,
                g.Count(),
                g.Count(x => x.RiskLevel is "High" or "Critical"),
                Math.Round(g.Average(x => x.ProgressPercent), 2)))
            .OrderByDescending(c => c.HighRiskStudents)
            .ThenBy(c => c.Cohort)
            .ToList();

        var professorPerformance = await _db.AcademicClinicalHistories
            .AsNoTracking()
            .Where(h => h.ReviewedByProfessorId != null)
            .GroupBy(h => h.ReviewedByProfessorId!.Value)
            .Select(g => new
            {
                ProfessorId = g.Key,
                Reviewed = g.Count(),
                Approved = g.Count(x => x.Status == ClinicalHistoryStatus.Approved),
                Rejected = g.Count(x => x.Status == ClinicalHistoryStatus.Rejected),
                AvgGrade = g.Where(x => x.Grade.HasValue).Average(x => x.Grade)
            })
            .ToListAsync();

        var professorIds = professorPerformance.Select(p => p.ProfessorId).Distinct().ToList();
        var professorNames = await _db.Users
            .AsNoTracking()
            .Where(u => professorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var professorMetrics = professorPerformance
            .Select(p => new ProfessorPerformanceMetricDto(
                p.ProfessorId,
                professorNames.TryGetValue(p.ProfessorId, out var name) ? name : "Profesor",
                p.Reviewed,
                p.Approved,
                p.Rejected,
                p.AvgGrade.HasValue ? Math.Round(p.AvgGrade.Value, 2) : null))
            .OrderByDescending(p => p.TotalReviewed)
            .ToList();

        return Ok(new AcademicAnalyticsDashboardDto(
            profiles.Count,
            highRisk,
            mediumRisk,
            approvalRate,
            historyStats?.Pending ?? 0,
            appointmentsStats?.Pending ?? 0,
            byCohort,
            profiles.OrderByDescending(p => p.RiskScore).Take(20).ToList(),
            professorMetrics));
    }

    [HttpGet("dropout-risk")]
    public async Task<ActionResult<IEnumerable<StudentRiskProfileDto>>> GetDropoutRiskProfiles()
    {
        var isAdmin = User.IsInRole(Roles.Admin);
        var profiles = await BuildRiskProfilesAsync(
            isAdmin ? null : await _scope.GetSupervisedStudentIdsAsync(GetUserId()));
        return Ok(profiles.OrderByDescending(p => p.RiskScore));
    }

    [HttpPost("risk-flags")]
    public async Task<ActionResult<RiskFlagDto>> CreateRiskFlag([FromBody] CreateRiskFlagRequest request)
    {
        var isAdmin = User.IsInRole(Roles.Admin);
        var actorId = GetUserId();

        if (request.StudentId == Guid.Empty || string.IsNullOrWhiteSpace(request.Notes))
        {
            return BadRequest(new { message = "StudentId y Notes son obligatorios." });
        }

        if (!isAdmin && !await _scope.ProfessorSupervisesStudentAsync(actorId, request.StudentId))
        {
            return Forbid();
        }

        var studentExists = await _db.Users.AnyAsync(u => u.Id == request.StudentId);
        if (!studentExists)
        {
            return NotFound(new { message = "No existe el estudiante." });
        }

        var item = new AcademicStudentRiskFlag
        {
            Id = Guid.NewGuid(),
            StudentId = request.StudentId,
            RiskLevel = request.RiskLevel,
            Notes = request.Notes.Trim(),
            IsResolved = false,
            CreatedByUserId = actorId,
            CreatedAt = DateTimeHelper.Now()
        };

        _db.AcademicStudentRiskFlags.Add(item);
        await _db.SaveChangesAsync();

        return Ok(MapRiskFlag(item));
    }

    [HttpPut("risk-flags/{id:guid}/resolve")]
    public async Task<ActionResult<RiskFlagDto>> ResolveRiskFlag(Guid id, [FromBody] ResolveRiskFlagRequest request)
    {
        var isAdmin = User.IsInRole(Roles.Admin);
        var actorId = GetUserId();

        var item = await _db.AcademicStudentRiskFlags.FirstOrDefaultAsync(f => f.Id == id);
        if (item == null)
        {
            return NotFound();
        }

        if (!isAdmin && !await _scope.ProfessorSupervisesStudentAsync(actorId, item.StudentId))
        {
            return Forbid();
        }

        if (item.IsResolved)
        {
            return BadRequest(new { message = "La bandera ya está resuelta." });
        }

        item.IsResolved = true;
        item.ResolvedAt = DateTimeHelper.Now();
        item.ResolvedByUserId = actorId;
        if (!string.IsNullOrWhiteSpace(request.ResolutionNotes))
        {
            item.Notes = $"{item.Notes}\nResuelto: {request.ResolutionNotes.Trim()}".Trim();
        }

        await _db.SaveChangesAsync();
        return Ok(MapRiskFlag(item));
    }

    private async Task<List<StudentRiskProfileDto>> BuildRiskProfilesAsync(IReadOnlyCollection<Guid>? allowedStudentIds = null)
    {
        var studentIds = await (
            from userRole in _db.UserRoles.AsNoTracking()
            join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.Name == Roles.Student
            select userRole.UserId)
            .Distinct()
            .ToListAsync();

        if (allowedStudentIds is not null)
        {
            var allowedSet = allowedStudentIds.ToHashSet();
            studentIds = studentIds.Where(id => allowedSet.Contains(id)).ToList();
        }

        if (studentIds.Count == 0)
        {
            return new List<StudentRiskProfileDto>();
        }

        var students = await _db.Users
            .AsNoTracking()
            .Where(u => studentIds.Contains(u.Id))
            .ToListAsync();

        var histories = await _db.AcademicClinicalHistories
            .AsNoTracking()
            .Where(h => studentIds.Contains(h.StudentId))
            .ToListAsync();

        var appointments = await _db.AcademicAppointments
            .AsNoTracking()
            .Where(a => studentIds.Contains(a.StudentId))
            .ToListAsync();

        var riskFlags = await _db.AcademicStudentRiskFlags
            .AsNoTracking()
            .Where(f => studentIds.Contains(f.StudentId) && !f.IsResolved)
            .ToListAsync();

        var now = DateTimeHelper.Now();
        var profiles = new List<StudentRiskProfileDto>();

        foreach (var student in students)
        {
            var studentHistories = histories.Where(h => h.StudentId == student.Id).ToList();
            var studentAppointments = appointments.Where(a => a.StudentId == student.Id).ToList();
            var unresolvedFlags = riskFlags.Where(f => f.StudentId == student.Id).ToList();

            var approved = studentHistories.Count(h => h.Status == ClinicalHistoryStatus.Approved);
            var rejected = studentHistories.Count(h => h.Status == ClinicalHistoryStatus.Rejected);
            var pending = studentHistories.Count(h => h.Status == ClinicalHistoryStatus.Submitted);
            var totalHistories = studentHistories.Count;
            var overdueAppointments = studentAppointments.Count(a => a.StartAt < now && a.Status == AppointmentStatus.Pending);
            var totalAppointments = studentAppointments.Count;
            var completedAppointments = studentAppointments.Count(a => a.Status == AppointmentStatus.Confirmed);

            decimal riskScore = 0m;
            if (totalHistories == 0)
            {
                riskScore += 3m;
            }

            riskScore += pending * 1.5m;
            riskScore += rejected * 2m;
            riskScore += overdueAppointments * 1.2m;

            var approvalRate = totalHistories > 0 ? (decimal)approved / totalHistories : 0m;
            if (totalHistories >= 3 && approvalRate < 0.5m)
            {
                riskScore += 2.5m;
            }

            var appointmentCompletion = totalAppointments > 0
                ? (decimal)completedAppointments / totalAppointments
                : 0m;
            if (totalAppointments >= 3 && appointmentCompletion < 0.6m)
            {
                riskScore += 1.5m;
            }

            var manualRisk = unresolvedFlags.Count > 0
                ? unresolvedFlags.Max(f => f.RiskLevel)
                : StudentRiskLevel.Low;
            riskScore += manualRisk switch
            {
                StudentRiskLevel.Low => 0.5m,
                StudentRiskLevel.Medium => 2m,
                StudentRiskLevel.High => 4m,
                StudentRiskLevel.Critical => 6m,
                _ => 0m
            };

            var progressPercent = totalHistories == 0
                ? 0m
                : Math.Clamp(((decimal)approved / totalHistories) * 100m, 0m, 100m);

            var riskLevel = riskScore switch
            {
                < 3m => "Low",
                < 6m => "Medium",
                < 9m => "High",
                _ => "Critical"
            };

            var cohort = ResolveCohort(student.UniversityId);

            profiles.Add(new StudentRiskProfileDto(
                student.Id,
                student.FullName,
                student.Email ?? string.Empty,
                cohort,
                totalHistories,
                approved,
                rejected,
                pending,
                totalAppointments,
                overdueAppointments,
                unresolvedFlags.Count,
                Math.Round(riskScore, 2),
                riskLevel,
                Math.Round(progressPercent, 2)));
        }

        return profiles;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static string ResolveCohort(string? universityId)
    {
        if (string.IsNullOrWhiteSpace(universityId))
        {
            return "General";
        }

        var trimmed = universityId.Trim().ToUpperInvariant();
        if (trimmed.Length < 3)
        {
            return trimmed;
        }

        return trimmed[..3];
    }

    private static RiskFlagDto MapRiskFlag(AcademicStudentRiskFlag item) => new(
        item.Id,
        item.StudentId,
        item.RiskLevel.ToString(),
        item.Notes,
        item.IsResolved,
        item.CreatedByUserId,
        item.CreatedAt,
        item.ResolvedAt,
        item.ResolvedByUserId);
}

public record AcademicAnalyticsDashboardDto(
    int TotalStudents,
    int HighRiskStudents,
    int MediumRiskStudents,
    decimal ApprovalRate,
    int PendingHistories,
    int PendingAppointments,
    IReadOnlyCollection<AcademicCohortMetricDto> CohortMetrics,
    IReadOnlyCollection<StudentRiskProfileDto> TopRiskStudents,
    IReadOnlyCollection<ProfessorPerformanceMetricDto> ProfessorPerformance);

public record AcademicCohortMetricDto(
    string Cohort,
    int Students,
    int HighRiskStudents,
    decimal AverageProgressPercent);

public record ProfessorPerformanceMetricDto(
    Guid ProfessorId,
    string ProfessorName,
    int TotalReviewed,
    int ApprovedReviewed,
    int RejectedReviewed,
    decimal? AverageGrade);

public record StudentRiskProfileDto(
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    string Cohort,
    int TotalHistories,
    int ApprovedHistories,
    int RejectedHistories,
    int PendingHistories,
    int TotalAppointments,
    int OverdueAppointments,
    int ActiveManualFlags,
    decimal RiskScore,
    string RiskLevel,
    decimal ProgressPercent);

public record RiskFlagDto(
    Guid Id,
    Guid StudentId,
    string RiskLevel,
    string Notes,
    bool IsResolved,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    Guid? ResolvedByUserId);

public record CreateRiskFlagRequest(
    Guid StudentId,
    StudentRiskLevel RiskLevel,
    string? Notes);

public record ResolveRiskFlagRequest(string? ResolutionNotes);
