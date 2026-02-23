using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MEDICSYS.Api.Contracts;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly OdontologoDbContext _odontologoDb;

    public AiController(OdontologoDbContext odontologoDb)
    {
        _odontologoDb = odontologoDb;
    }

    [Authorize]
    [HttpPost("suggest-notes")]
    public ActionResult<AiSuggestResponse> SuggestNotes(AiSuggestRequest request)
    {
        var summary = BuildSuggestion(request);
        return Ok(new AiSuggestResponse { Suggestion = summary });
    }

    [Authorize(Roles = $"{Roles.Odontologo},{Roles.Professor},{Roles.Admin}")]
    [HttpPost("suggest-diagnosis")]
    public ActionResult<object> SuggestDiagnosis([FromBody] AiDiagnosisRequest request)
    {
        var corpus = $"{request.Symptoms} {request.ClinicalFindings} {request.Notes}".ToLowerInvariant();
        var suggestions = new List<AiDiagnosisSuggestion>();

        void AddIf(bool condition, string diagnosis, decimal confidence, string rationale)
        {
            if (condition)
            {
                suggestions.Add(new AiDiagnosisSuggestion(diagnosis, confidence, rationale));
            }
        }

        AddIf(corpus.Contains("dolor") && corpus.Contains("frío"), "Pulpitis reversible", 0.72m, "Dolor asociado a estímulos térmicos.");
        AddIf(corpus.Contains("dolor") && corpus.Contains("constante"), "Pulpitis irreversible", 0.76m, "Dolor persistente no autolimitado.");
        AddIf(corpus.Contains("caries"), "Caries dental activa", 0.83m, "Evidencia de lesión cariosa.");
        AddIf(corpus.Contains("sangrado") || corpus.Contains("encía"), "Gingivitis", 0.74m, "Compromiso gingival y sangrado reportado.");
        AddIf(corpus.Contains("movilidad") || corpus.Contains("periodontal"), "Enfermedad periodontal", 0.78m, "Signos compatibles con pérdida de soporte periodontal.");
        AddIf(corpus.Contains("absceso") || corpus.Contains("inflamación"), "Absceso dentoalveolar", 0.71m, "Se describe inflamación localizada.");
        AddIf(corpus.Contains("trauma") || corpus.Contains("fractura"), "Trauma dentoalveolar", 0.69m, "Antecedente traumático en pieza dental.");

        if (suggestions.Count == 0)
        {
            suggestions.Add(new AiDiagnosisSuggestion(
                "Evaluación clínica complementaria requerida",
                0.55m,
                "La información actual no permite una hipótesis principal robusta."));
        }

        var ranked = suggestions
            .OrderByDescending(s => s.Confidence)
            .Take(5)
            .ToList();

        return Ok(new
        {
            PrimarySuggestion = ranked.First(),
            DifferentialDiagnoses = ranked,
            RecommendedActions = new[]
            {
                "Corroborar hallazgos con examen clínico completo.",
                "Solicitar imágenes diagnósticas cuando sea necesario.",
                "Registrar plan terapéutico y control evolutivo."
            },
            Disclaimer = "Sugerencia automatizada de apoyo. No reemplaza criterio clínico profesional."
        });
    }

    [Authorize(Roles = $"{Roles.Odontologo},{Roles.Admin}")]
    [HttpGet("predictive-trends")]
    public async Task<ActionResult<object>> GetPredictiveTrends([FromQuery] int months = 6)
    {
        var normalizedMonths = Math.Clamp(months, 1, 24);
        var now = DateTimeHelper.Now();
        var start = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc)
            .AddMonths(-(normalizedMonths - 1));

        var odontologoId = GetOdontologoIdIfPresent();
        var appointmentsQuery = _odontologoDb.OdontologoAppointments
            .AsNoTracking()
            .Where(a => a.StartAt >= start && a.StartAt <= now);

        var historiesQuery = _odontologoDb.OdontologoClinicalHistories
            .AsNoTracking()
            .Where(h => h.CreatedAt >= start && h.CreatedAt <= now);

        var claimsQuery = _odontologoDb.InsuranceClaims
            .AsNoTracking()
            .Where(c => c.RequestedAt >= start && c.RequestedAt <= now);

        if (odontologoId.HasValue)
        {
            appointmentsQuery = appointmentsQuery.Where(a => a.OdontologoId == odontologoId.Value);
            historiesQuery = historiesQuery.Where(h => h.OdontologoId == odontologoId.Value);
            claimsQuery = claimsQuery.Where(c => c.OdontologoId == odontologoId.Value);
        }

        var reasons = await appointmentsQuery
            .Select(a => a.Reason)
            .ToListAsync();

        var topPatterns = reasons
            .Select(ClassifyPattern)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .GroupBy(p => p)
            .Select(g => new
            {
                Pattern = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToList();

        var monthlyLoadRaw = await appointmentsQuery
            .GroupBy(a => new { a.StartAt.Year, a.StartAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
            })
            .ToListAsync();

        var monthlyLoad = Enumerable.Range(0, normalizedMonths)
            .Select(offset =>
            {
                var month = start.AddMonths(offset);
                var match = monthlyLoadRaw.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month);
                var count = match?.Count ?? 0;
                return new
                {
                    Month = $"{month:yyyy-MM}",
                    AppointmentCount = count
                };
            })
            .ToList();

        var recentTrend = monthlyLoad.TakeLast(3).Average(x => x.AppointmentCount);
        var demandForecast = Math.Round(recentTrend * 1.08, 0);

        var historiesCount = await historiesQuery.CountAsync();
        var approvedClaims = await claimsQuery.CountAsync(c => c.Status == MEDICSYS.Api.Models.Odontologia.InsuranceClaimStatus.Approved);
        var totalClaims = await claimsQuery.CountAsync();

        return Ok(new
        {
            Period = new { Start = start, End = now, Months = normalizedMonths },
            TopClinicalPatterns = topPatterns,
            AppointmentLoadByMonth = monthlyLoad,
            HistoryRecordsAnalyzed = historiesCount,
            InsuranceApprovalRate = totalClaims > 0 ? Math.Round((decimal)approvedClaims * 100m / totalClaims, 2) : 0m,
            Forecast = new
            {
                NextMonthExpectedAppointments = demandForecast,
                Basis = "Promedio móvil de los últimos 3 meses con ajuste del 8%."
            },
            Disclaimer = "Análisis predictivo basado en datos anonimizados agregados del sistema."
        });
    }

    private static string BuildSuggestion(AiSuggestRequest request)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            parts.Add($"Motivo: {request.Reason}.");
        }
        if (!string.IsNullOrWhiteSpace(request.CurrentIssue))
        {
            parts.Add($"Problema actual: {request.CurrentIssue}.");
        }
        if (!string.IsNullOrWhiteSpace(request.Plan))
        {
            parts.Add($"Plan: {request.Plan}.");
        }
        if (!string.IsNullOrWhiteSpace(request.Procedures))
        {
            parts.Add($"Procedimientos: {request.Procedures}.");
        }
        if (parts.Count == 0 && !string.IsNullOrWhiteSpace(request.Notes))
        {
            parts.Add(request.Notes);
        }
        if (parts.Count == 0)
        {
            parts.Add("Sin observaciones relevantes. Se recomienda control y seguimiento.");
        }
        return string.Join(" ", parts);
    }

    private Guid? GetOdontologoIdIfPresent()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(id, out var parsed) && User.IsInRole(Roles.Odontologo))
        {
            return parsed;
        }

        return null;
    }

    private static string ClassifyPattern(string reason)
    {
        var value = reason.ToLowerInvariant();

        if (value.Contains("caries"))
        {
            return "Caries";
        }
        if (value.Contains("profilaxis") || value.Contains("limpieza"))
        {
            return "Profilaxis";
        }
        if (value.Contains("dolor"))
        {
            return "Dolor dental";
        }
        if (value.Contains("endodoncia"))
        {
            return "Endodoncia";
        }
        if (value.Contains("extracción") || value.Contains("extraccion"))
        {
            return "Extracción";
        }
        if (value.Contains("ortodoncia"))
        {
            return "Ortodoncia";
        }
        if (value.Contains("periodontal") || value.Contains("encía") || value.Contains("encia"))
        {
            return "Periodoncia";
        }

        return "General";
    }
}

public class AiSuggestResponse
{
    public string Suggestion { get; set; } = string.Empty;
}

public record AiDiagnosisRequest(
    string Symptoms,
    string ClinicalFindings,
    string? Notes);

public record AiDiagnosisSuggestion(
    string Diagnosis,
    decimal Confidence,
    string Rationale);
