using System.Security.Claims;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models.Academico;

namespace MEDICSYS.Api.Services;

public class AcademicAuditLogger
{
    private readonly AcademicDbContext _db;
    private readonly ILogger<AcademicAuditLogger> _logger;

    public AcademicAuditLogger(AcademicDbContext db, ILogger<AcademicAuditLogger> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task TryRecordAsync(HttpContext context)
    {
        try
        {
            if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/api/health", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (HttpMethods.IsOptions(context.Request.Method))
            {
                return;
            }

            var userIdRaw = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.TryParse(userIdRaw, out var parsedUserId) ? parsedUserId : (Guid?)null;
            var userEmail = context.User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(userEmail) && userId.HasValue)
            {
                userEmail = await _db.Users
                    .Where(u => u.Id == userId.Value)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();
            }

            var role = context.User.FindFirstValue(ClaimTypes.Role);
            var subjectType = context.Request.Query.TryGetValue("subjectType", out var subjectTypeValue)
                ? subjectTypeValue.ToString()
                : null;
            var subjectIdentifier = context.Request.Query.TryGetValue("subjectId", out var subjectIdValue)
                ? subjectIdValue.ToString()
                : null;

            var eventType = ResolveEventType(context.Request.Method, context.Request.Path, context.Response.StatusCode);

            _db.AcademicDataAuditEvents.Add(new AcademicDataAuditEvent
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                Path = context.Request.Path.ToString(),
                Method = context.Request.Method,
                StatusCode = context.Response.StatusCode,
                UserId = userId,
                UserEmail = string.IsNullOrWhiteSpace(userEmail) ? null : userEmail.Trim(),
                UserRole = role,
                SubjectType = string.IsNullOrWhiteSpace(subjectType) ? null : subjectType.Trim(),
                SubjectIdentifier = string.IsNullOrWhiteSpace(subjectIdentifier) ? null : subjectIdentifier.Trim(),
                Reason = context.Request.Query.TryGetValue("reason", out var reason) ? reason.ToString() : null,
                IpAddress = MaskIpAddress(context.Connection.RemoteIpAddress?.ToString()),
                OccurredAt = DateTimeHelper.Now()
            });

            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo registrar el evento de auditoria academica.");
        }
    }

    private static DataAuditEventType ResolveEventType(string method, PathString path, int statusCode)
    {
        var pathValue = path.Value ?? string.Empty;

        if (pathValue.Contains("/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            return DataAuditEventType.Login;
        }

        if (pathValue.Contains("/export", StringComparison.OrdinalIgnoreCase))
        {
            return DataAuditEventType.Export;
        }

        if (statusCode == StatusCodes.Status401Unauthorized || statusCode == StatusCodes.Status403Forbidden)
        {
            return DataAuditEventType.Other;
        }

        return method.ToUpperInvariant() switch
        {
            "GET" => DataAuditEventType.Read,
            "POST" => DataAuditEventType.Create,
            "PUT" or "PATCH" => DataAuditEventType.Update,
            "DELETE" => DataAuditEventType.Delete,
            _ => DataAuditEventType.Other
        };
    }

    private static string? MaskIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        if (ipAddress.Contains('.'))
        {
            var parts = ipAddress.Split('.');
            if (parts.Length == 4)
            {
                parts[3] = "xxx";
                return string.Join('.', parts);
            }
        }

        if (ipAddress.Contains(':'))
        {
            var parts = ipAddress.Split(':');
            if (parts.Length > 2)
            {
                return string.Join(':', parts.Take(Math.Min(4, parts.Length))) + ":xxxx";
            }
        }

        return ipAddress;
    }
}
