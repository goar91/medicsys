using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models.Odontologia;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Controllers.Odontologia;

[ApiController]
[Route("api/odontologia/seguros")]
[Authorize(Roles = Roles.Odontologo)]
public class SegurosController : ControllerBase
{
    private readonly OdontologoDbContext _db;

    public SegurosController(OdontologoDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("validar-cobertura")]
    public async Task<ActionResult<object>> ValidateCoverage([FromBody] InsuranceCoverageValidationRequest request)
    {
        var odontologoId = GetUserId();

        var patientExists = await _db.OdontologoPatients
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.PatientId && p.OdontologoId == odontologoId);

        if (!patientExists)
        {
            return BadRequest("Paciente no encontrado.");
        }

        var normalizedPolicy = request.PolicyNumber.Trim().ToUpperInvariant();
        var insurer = request.InsurerName.Trim();
        var amount = request.RequestedAmount;

        // Simulación determinista mientras se completa integración directa con aseguradoras.
        var score = normalizedPolicy
            .Where(char.IsLetterOrDigit)
            .Select(c => (int)c)
            .Sum();

        var coveragePercent = 50 + (score % 41); // 50% a 90%
        var maxCoverage = Math.Round(amount * (coveragePercent / 100m), 2);
        var approved = coveragePercent >= 60;

        return Ok(new
        {
            Insurer = insurer,
            PolicyNumber = normalizedPolicy,
            ProcedureCode = request.ProcedureCode,
            RequestedAmount = amount,
            CoveragePercent = coveragePercent,
            CoveredAmount = approved ? maxCoverage : 0m,
            IsApproved = approved,
            Message = approved
                ? "Cobertura preliminar válida. Se puede emitir reclamo."
                : "Cobertura insuficiente para este procedimiento."
        });
    }

    [HttpGet("reclamaciones")]
    public async Task<ActionResult<IEnumerable<object>>> GetClaims([FromQuery] Guid? patientId, [FromQuery] string? status)
    {
        var odontologoId = GetUserId();

        var query = _db.InsuranceClaims
            .AsNoTracking()
            .Where(c => c.OdontologoId == odontologoId);

        if (patientId.HasValue)
        {
            query = query.Where(c => c.PatientId == patientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InsuranceClaimStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(c => c.Status == parsedStatus);
        }

        var claims = await query
            .OrderByDescending(c => c.RequestedAt)
            .Take(200)
            .ToListAsync();

        return Ok(claims.Select(MapClaim));
    }

    [HttpPost("reclamaciones")]
    public async Task<ActionResult<object>> CreateClaim([FromBody] CreateInsuranceClaimRequest request)
    {
        var odontologoId = GetUserId();

        var patientExists = await _db.OdontologoPatients
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.PatientId && p.OdontologoId == odontologoId);

        if (!patientExists)
        {
            return BadRequest("Paciente no encontrado.");
        }

        var validation = SimulateCoverage(request.PolicyNumber, request.RequestedAmount);

        var claim = new InsuranceClaim
        {
            Id = Guid.NewGuid(),
            OdontologoId = odontologoId,
            PatientId = request.PatientId,
            InsurerName = request.InsurerName.Trim(),
            PolicyNumber = request.PolicyNumber.Trim(),
            ProcedureCode = request.ProcedureCode.Trim(),
            ProcedureDescription = request.ProcedureDescription.Trim(),
            RequestedAmount = request.RequestedAmount,
            ApprovedAmount = validation.IsApproved ? validation.CoveredAmount : null,
            Status = validation.IsApproved ? InsuranceClaimStatus.Approved : InsuranceClaimStatus.NeedsInformation,
            ResponseMessage = validation.Message,
            RequestedAt = DateTimeHelper.Now(),
            ResolvedAt = validation.IsApproved ? DateTimeHelper.Now() : null
        };

        _db.InsuranceClaims.Add(claim);
        await _db.SaveChangesAsync();

        return Ok(MapClaim(claim));
    }

    [HttpPut("reclamaciones/{id:guid}/estado")]
    public async Task<ActionResult<object>> UpdateClaimStatus(Guid id, [FromBody] UpdateInsuranceClaimStatusRequest request)
    {
        var odontologoId = GetUserId();

        var claim = await _db.InsuranceClaims
            .FirstOrDefaultAsync(c => c.Id == id && c.OdontologoId == odontologoId);

        if (claim == null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<InsuranceClaimStatus>(request.Status, true, out var status))
        {
            return BadRequest("Estado inválido.");
        }

        claim.Status = status;
        claim.ResponseMessage = request.ResponseMessage?.Trim();
        claim.ApprovedAmount = request.ApprovedAmount;
        claim.ResolvedAt = status == InsuranceClaimStatus.Pending ? null : DateTimeHelper.Now();

        await _db.SaveChangesAsync();

        return Ok(MapClaim(claim));
    }

    private static object MapClaim(InsuranceClaim claim)
    {
        return new
        {
            claim.Id,
            claim.PatientId,
            claim.InsurerName,
            claim.PolicyNumber,
            claim.ProcedureCode,
            claim.ProcedureDescription,
            claim.RequestedAmount,
            claim.ApprovedAmount,
            Status = claim.Status.ToString(),
            claim.ResponseMessage,
            claim.RequestedAt,
            claim.ResolvedAt
        };
    }

    private static (bool IsApproved, decimal CoveredAmount, string Message) SimulateCoverage(string policyNumber, decimal requestedAmount)
    {
        var normalized = policyNumber.Trim().ToUpperInvariant();
        var score = normalized
            .Where(char.IsLetterOrDigit)
            .Select(c => (int)c)
            .Sum();

        var coveragePercent = 50 + (score % 41);
        var approved = coveragePercent >= 60;
        var coveredAmount = approved ? Math.Round(requestedAmount * coveragePercent / 100m, 2) : 0m;

        return approved
            ? (true, coveredAmount, $"Cobertura aprobada al {coveragePercent}%.")
            : (false, 0m, "La póliza requiere documentación adicional o plan complementario.");
    }
}

public record InsuranceCoverageValidationRequest(
    Guid PatientId,
    string InsurerName,
    string PolicyNumber,
    string ProcedureCode,
    decimal RequestedAmount);

public record CreateInsuranceClaimRequest(
    Guid PatientId,
    string InsurerName,
    string PolicyNumber,
    string ProcedureCode,
    string ProcedureDescription,
    decimal RequestedAmount);

public record UpdateInsuranceClaimStatusRequest(
    string Status,
    decimal? ApprovedAmount,
    string? ResponseMessage);
