using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models.Academico;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Controllers;

[ApiController]
[Route("api/auditoria")]
[Authorize(Roles = $"{Roles.Auditoria},{Roles.Admin}")]
public class AuditoriaController : ControllerBase
{
    private readonly AcademicDbContext _db;

    public AuditoriaController(AcademicDbContext db)
    {
        _db = db;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<AuditoriaDashboardDto>> GetDashboard([FromQuery] int days = 30)
    {
        var now = DateTimeHelper.Now();
        var normalizedDays = Math.Clamp(days, 1, 365);
        var from = now.AddDays(-normalizedDays);

        var events = await _db.AcademicDataAuditEvents
            .AsNoTracking()
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= now)
            .ToListAsync();

        var retentionMonths = await _db.AcademicDataRetentionPolicies
            .AsNoTracking()
            .Where(p => p.IsActive && p.DataCategory == "AuditTrailAcademico")
            .Select(p => (int?)p.RetentionMonths)
            .FirstOrDefaultAsync() ?? 60;

        var roleSummaries = events
            .GroupBy(e => string.IsNullOrWhiteSpace(e.UserRole) ? "SinRol" : e.UserRole!)
            .Select(g => new AuditoriaRoleSummaryDto(
                g.Key,
                g.Count(),
                g.Count(e => e.StatusCode >= 400),
                g.Count(e => IsSensitivePath(e.Path)),
                g.Max(e => e.OccurredAt)))
            .OrderByDescending(x => x.TotalEvents)
            .ToList();

        foreach (var role in new[] { Roles.Admin, Roles.Auditoria, Roles.Professor, Roles.Student, Roles.Odontologo })
        {
            if (roleSummaries.All(r => r.Role != role))
            {
                roleSummaries.Add(new AuditoriaRoleSummaryDto(role, 0, 0, 0, null));
            }
        }

        var moduleSummaries = events
            .GroupBy(e => ResolveModule(e.Path))
            .Select(g => new AuditoriaModuleSummaryDto(
                g.Key,
                g.Count(),
                g.Count(e => e.StatusCode >= 400),
                g.Count(e => IsSensitivePath(e.Path))))
            .OrderByDescending(x => x.TotalEvents)
            .ToList();

        var unauthorizedAttempts = events.Count(e => e.StatusCode == StatusCodes.Status401Unauthorized || e.StatusCode == StatusCodes.Status403Forbidden);
        var failedEvents = events.Count(e => e.StatusCode >= 400);
        var sensitiveAccesses = events.Count(e => IsSensitivePath(e.Path));
        var activeActors = events
            .Select(e => e.UserId?.ToString() ?? e.UserEmail ?? e.IpAddress)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var riskAlerts = new List<string>();
        if (unauthorizedAttempts >= 10)
        {
            riskAlerts.Add($"Intentos no autorizados elevados: {unauthorizedAttempts} en {normalizedDays} día(s).");
        }
        if (events.Count(e => IsSensitivePath(e.Path) && e.StatusCode >= 400) >= 5)
        {
            riskAlerts.Add("Accesos fallidos sobre datos sensibles detectados.");
        }
        if (events.Count(e => e.EventType == DataAuditEventType.Delete && IsSensitivePath(e.Path)) >= 3)
        {
            riskAlerts.Add("Operaciones de eliminación en datos sensibles por encima del umbral.");
        }

        var legalReferences = new List<string>
        {
            "Constitución del Ecuador, Art. 66 (derecho a la protección de datos personales).",
            "Ley Orgánica de Protección de Datos Personales (LOPDP, 2021).",
            "Reglamento General a la LOPDP (Decreto Ejecutivo 904, 2023)."
        };

        return Ok(new AuditoriaDashboardDto(
            from,
            now,
            events.Count,
            failedEvents,
            unauthorizedAttempts,
            sensitiveAccesses,
            activeActors,
            retentionMonths,
            roleSummaries.OrderByDescending(x => x.TotalEvents).ToList(),
            moduleSummaries,
            riskAlerts,
            legalReferences));
    }

    [HttpGet("eventos")]
    public async Task<ActionResult<AuditoriaEventsPageDto>> GetEventos([FromQuery] AuditoriaEventsQuery query)
    {
        var now = DateTimeHelper.Now();
        var normalizedDays = Math.Clamp(query.Days, 1, 365);
        var from = query.From?.ToUniversalTime() ?? now.AddDays(-normalizedDays);
        var to = query.To?.ToUniversalTime() ?? now;
        if (to < from)
        {
            return BadRequest(new { message = "El rango de fechas es inválido." });
        }

        var eventsQuery = _db.AcademicDataAuditEvents
            .AsNoTracking()
            .Where(e => e.OccurredAt >= from && e.OccurredAt <= to);

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = query.Role.Trim();
            eventsQuery = eventsQuery.Where(e => e.UserRole == role);
        }

        if (!string.IsNullOrWhiteSpace(query.Method))
        {
            var method = query.Method.Trim().ToUpperInvariant();
            eventsQuery = eventsQuery.Where(e => e.Method == method);
        }

        if (!string.IsNullOrWhiteSpace(query.EventType) &&
            Enum.TryParse<DataAuditEventType>(query.EventType, true, out var parsedEventType))
        {
            eventsQuery = eventsQuery.Where(e => e.EventType == parsedEventType);
        }

        if (query.StatusCodeFrom.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => e.StatusCode >= query.StatusCodeFrom.Value);
        }

        if (query.StatusCodeTo.HasValue)
        {
            eventsQuery = eventsQuery.Where(e => e.StatusCode <= query.StatusCodeTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Module))
        {
            eventsQuery = ApplyModuleFilter(eventsQuery, query.Module.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            eventsQuery = eventsQuery.Where(e =>
                EF.Functions.ILike(e.Path, term) ||
                (e.UserEmail != null && EF.Functions.ILike(e.UserEmail, term)) ||
                (e.SubjectIdentifier != null && EF.Functions.ILike(e.SubjectIdentifier, term)));
        }

        var total = await eventsQuery.CountAsync();
        var take = Math.Clamp(query.Take, 1, 500);
        var skip = Math.Max(0, query.Skip);
        var includePersonalData = query.IncludePersonalData && User.IsInRole(Roles.Admin);

        var items = await eventsQuery
            .OrderByDescending(e => e.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var mapped = items.Select(e => new AuditoriaEventDto(
            e.Id,
            e.OccurredAt,
            ResolveModule(e.Path),
            e.EventType.ToString(),
            e.Method,
            e.Path,
            e.StatusCode,
            string.IsNullOrWhiteSpace(e.UserRole) ? "SinRol" : e.UserRole!,
            includePersonalData ? e.UserEmail : MaskEmail(e.UserEmail),
            e.SubjectType,
            includePersonalData ? e.SubjectIdentifier : MaskIdentifier(e.SubjectIdentifier),
            e.IpAddress,
            IsSensitivePath(e.Path)))
            .ToList();

        return Ok(new AuditoriaEventsPageDto(from, to, total, skip, take, mapped));
    }

    private static IQueryable<AcademicDataAuditEvent> ApplyModuleFilter(IQueryable<AcademicDataAuditEvent> query, string module)
    {
        return module.ToLowerInvariant() switch
        {
            "academico" => query.Where(e => e.Path.StartsWith("/api/academic")),
            "odontologia" => query.Where(e => e.Path.StartsWith("/api/odontologia")),
            "autenticacion" => query.Where(e => e.Path.StartsWith("/api/auth")),
            "usuarios" => query.Where(e => e.Path.StartsWith("/api/users")),
            "facturacioncontabilidad" => query.Where(e =>
                e.Path.StartsWith("/api/invoices") ||
                e.Path.StartsWith("/api/accounting") ||
                e.Path.StartsWith("/api/sri")),
            "clinico" => query.Where(e =>
                e.Path.StartsWith("/api/patients") ||
                e.Path.StartsWith("/api/clinical-histories")),
            "agenda" => query.Where(e => e.Path.StartsWith("/api/agenda")),
            "ia" => query.Where(e => e.Path.StartsWith("/api/ai")),
            "auditoria" => query.Where(e => e.Path.StartsWith("/api/auditoria")),
            _ => query
        };
    }

    private static string ResolveModule(string path)
    {
        if (path.StartsWith("/api/academic", StringComparison.OrdinalIgnoreCase)) return "Academico";
        if (path.StartsWith("/api/odontologia", StringComparison.OrdinalIgnoreCase)) return "Odontologia";
        if (path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase)) return "Autenticacion";
        if (path.StartsWith("/api/users", StringComparison.OrdinalIgnoreCase)) return "Usuarios";
        if (path.StartsWith("/api/accounting", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/invoices", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/sri", StringComparison.OrdinalIgnoreCase)) return "FacturacionContabilidad";
        if (path.StartsWith("/api/patients", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/clinical-histories", StringComparison.OrdinalIgnoreCase)) return "Clinico";
        if (path.StartsWith("/api/agenda", StringComparison.OrdinalIgnoreCase)) return "Agenda";
        if (path.StartsWith("/api/ai", StringComparison.OrdinalIgnoreCase)) return "IA";
        if (path.StartsWith("/api/auditoria", StringComparison.OrdinalIgnoreCase)) return "Auditoria";
        return "General";
    }

    private static bool IsSensitivePath(string path)
    {
        return path.Contains("/patients", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/clinical-histories", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/invoices", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/seguros", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/portal-paciente", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/documentos-firmados", StringComparison.OrdinalIgnoreCase);
    }

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var at = email.IndexOf('@');
        if (at <= 1)
        {
            return "***";
        }

        var local = email[..at];
        var domain = email[(at + 1)..];
        return $"{local[0]}***@{domain}";
    }

    private static string? MaskIdentifier(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        if (identifier.Length <= 4)
        {
            return "***";
        }

        return $"{identifier[..2]}***{identifier[^2..]}";
    }
}

public record AuditoriaDashboardDto(
    DateTime From,
    DateTime To,
    int TotalEvents,
    int FailedEvents,
    int UnauthorizedAttempts,
    int SensitiveDataAccesses,
    int ActiveActors,
    int RetentionMonths,
    IReadOnlyCollection<AuditoriaRoleSummaryDto> RoleSummaries,
    IReadOnlyCollection<AuditoriaModuleSummaryDto> ModuleSummaries,
    IReadOnlyCollection<string> RiskAlerts,
    IReadOnlyCollection<string> LegalReferences);

public record AuditoriaRoleSummaryDto(
    string Role,
    int TotalEvents,
    int FailedEvents,
    int SensitiveDataAccesses,
    DateTime? LastActivityAt);

public record AuditoriaModuleSummaryDto(
    string Module,
    int TotalEvents,
    int FailedEvents,
    int SensitiveDataAccesses);

public record AuditoriaEventDto(
    Guid Id,
    DateTime OccurredAt,
    string Module,
    string EventType,
    string Method,
    string Path,
    int StatusCode,
    string ActorRole,
    string? ActorEmail,
    string? SubjectType,
    string? SubjectIdentifier,
    string? IpAddress,
    bool IsSensitiveAccess);

public record AuditoriaEventsPageDto(
    DateTime From,
    DateTime To,
    int Total,
    int Skip,
    int Take,
    IReadOnlyCollection<AuditoriaEventDto> Items);

public class AuditoriaEventsQuery
{
    public int Days { get; set; } = 30;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Role { get; set; }
    public string? Module { get; set; }
    public string? Method { get; set; }
    public string? EventType { get; set; }
    public int? StatusCodeFrom { get; set; }
    public int? StatusCodeTo { get; set; }
    public string? Search { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 100;
    public bool IncludePersonalData { get; set; }
}
