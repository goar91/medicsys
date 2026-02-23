using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models.Odontologia;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Controllers.Odontologia;

[ApiController]
[Route("api/odontologia/documentos-firmados")]
[Authorize(Roles = Roles.Odontologo)]
public class DocumentosFirmadosController : ControllerBase
{
    private readonly OdontologoDbContext _db;

    public DocumentosFirmadosController(OdontologoDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetDocuments([FromQuery] Guid? patientId)
    {
        var odontologoId = GetUserId();

        var query = _db.SignedClinicalDocuments
            .AsNoTracking()
            .Where(d => d.OdontologoId == odontologoId);

        if (patientId.HasValue)
        {
            query = query.Where(d => d.PatientId == patientId.Value);
        }

        var documents = await query
            .OrderByDescending(d => d.SignedAt)
            .Take(200)
            .ToListAsync();

        return Ok(documents.Select(Map));
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateDocument([FromBody] CreateSignedDocumentRequest request)
    {
        var odontologoId = GetUserId();

        var patientExists = await _db.OdontologoPatients
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.PatientId && p.OdontologoId == odontologoId);

        if (!patientExists)
        {
            return BadRequest("Paciente no encontrado.");
        }

        var normalizedPayload = string.IsNullOrWhiteSpace(request.DocumentContent)
            ? $"{request.DocumentType}|{request.DocumentName}|{request.Notes}"
            : request.DocumentContent;

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPayload));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var signedAt = request.SignedAt.HasValue
            ? NormalizeUtc(request.SignedAt.Value)
            : DateTimeHelper.Now();

        var document = new SignedClinicalDocument
        {
            Id = Guid.NewGuid(),
            OdontologoId = odontologoId,
            PatientId = request.PatientId,
            DocumentType = request.DocumentType.Trim(),
            DocumentName = request.DocumentName.Trim(),
            DocumentHash = hash,
            SignatureProvider = request.SignatureProvider.Trim(),
            SignatureSerial = string.IsNullOrWhiteSpace(request.SignatureSerial) ? null : request.SignatureSerial.Trim(),
            SignedAt = signedAt,
            ValidUntil = request.ValidUntil.HasValue ? NormalizeUtc(request.ValidUntil.Value) : null,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        _db.SignedClinicalDocuments.Add(document);
        await _db.SaveChangesAsync();

        return Ok(Map(document));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<object>> GetById(Guid id)
    {
        var odontologoId = GetUserId();

        var document = await _db.SignedClinicalDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id && d.OdontologoId == odontologoId);

        if (document == null)
        {
            return NotFound();
        }

        return Ok(Map(document));
    }

    private static object Map(SignedClinicalDocument document)
    {
        return new
        {
            document.Id,
            document.PatientId,
            document.DocumentType,
            document.DocumentName,
            document.DocumentHash,
            document.SignatureProvider,
            document.SignatureSerial,
            document.SignedAt,
            document.ValidUntil,
            document.Notes
        };
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}

public record CreateSignedDocumentRequest(
    Guid PatientId,
    string DocumentType,
    string DocumentName,
    string SignatureProvider,
    string? SignatureSerial,
    string? DocumentContent,
    string? Notes,
    DateTime? SignedAt,
    DateTime? ValidUntil);
