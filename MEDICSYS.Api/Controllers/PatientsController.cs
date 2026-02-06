using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Contracts;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize(Roles = Roles.Odontologo)]
public class PatientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PatientsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientDto>>> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var userId = GetUserId();
        var isOdontologo = User.IsInRole(Roles.Odontologo);
        
        var query = _db.Patients.AsNoTracking();
        
        if (isOdontologo)
        {
            query = query.Where(p => p.OdontologoId == userId);
        }
        
        var total = await query.CountAsync();

        if (page.HasValue || pageSize.HasValue)
        {
            var pageValue = Math.Max(1, page ?? 1);
            var sizeValue = Math.Clamp(pageSize ?? 50, 1, 200);
            query = query
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Skip((pageValue - 1) * sizeValue)
                .Take(sizeValue);

            Response.Headers["X-Total-Count"] = total.ToString();
            Response.Headers["X-Page"] = pageValue.ToString();
            Response.Headers["X-Page-Size"] = sizeValue.ToString();
        }
        else
        {
            query = query
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName);
        }

        var patients = await query.ToListAsync();
        return Ok(patients.Select(Map));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id)
    {
        var userId = GetUserId();
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (patient == null)
        {
            return NotFound();
        }
        
        // Verificar permisos
        var isOdontologo = User.IsInRole(Roles.Odontologo);
        if (isOdontologo && patient.OdontologoId != userId)
        {
            return Forbid();
        }
        
        return Ok(Map(patient));
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<PatientDto>>> Search([FromQuery] string q)
    {
        var userId = GetUserId();
        var isOdontologo = User.IsInRole(Roles.Odontologo);
        
        var query = _db.Patients.AsNoTracking();
        
        if (isOdontologo)
        {
            query = query.Where(p => p.OdontologoId == userId);
        }
        
        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = q.ToLower();
            query = query.Where(p => 
                p.FirstName.ToLower().Contains(searchTerm) ||
                p.LastName.ToLower().Contains(searchTerm) ||
                p.IdNumber.Contains(searchTerm) ||
                (p.Email ?? string.Empty).ToLower().Contains(searchTerm)
            );
        }
        
        var patients = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Take(20)
            .ToListAsync();
            
        return Ok(patients.Select(Map));
    }

    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create([FromBody] PatientCreateRequest request)
    {
        var userId = GetUserId();
        
        // Verificar si ya existe un paciente con ese número de cédula
        var existing = await _db.Patients
            .FirstOrDefaultAsync(p => p.IdNumber == request.IdNumber);
            
        if (existing != null)
        {
            return BadRequest(new { message = "Ya existe un paciente con ese número de cédula" });
        }
        
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            OdontologoId = userId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IdNumber = request.IdNumber,
            DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc),
            Gender = request.Gender,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email ?? string.Empty,
            EmergencyContact = request.EmergencyContact,
            EmergencyPhone = request.EmergencyPhone,
            Allergies = request.Allergies,
            Medications = request.Medications,
            Diseases = request.Diseases,
            BloodType = request.BloodType,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, Map(patient));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PatientDto>> Update(Guid id, [FromBody] PatientUpdateRequest request)
    {
        var userId = GetUserId();
        var patient = await _db.Patients.FindAsync(id);
        
        if (patient == null)
        {
            return NotFound();
        }
        
        // Verificar permisos
        var isOdontologo = User.IsInRole(Roles.Odontologo);
        if (isOdontologo && patient.OdontologoId != userId)
        {
            return Forbid();
        }
        
        // Actualizar campos
        if (!string.IsNullOrWhiteSpace(request.FirstName))
            patient.FirstName = request.FirstName;
        if (!string.IsNullOrWhiteSpace(request.LastName))
            patient.LastName = request.LastName;
        if (!string.IsNullOrWhiteSpace(request.Address))
            patient.Address = request.Address;
        if (!string.IsNullOrWhiteSpace(request.Phone))
            patient.Phone = request.Phone;
        if (!string.IsNullOrWhiteSpace(request.Email))
            patient.Email = request.Email;
        if (request.EmergencyContact != null)
            patient.EmergencyContact = request.EmergencyContact;
        if (request.EmergencyPhone != null)
            patient.EmergencyPhone = request.EmergencyPhone;
        if (request.Allergies != null)
            patient.Allergies = request.Allergies;
        if (request.Medications != null)
            patient.Medications = request.Medications;
        if (request.Diseases != null)
            patient.Diseases = request.Diseases;
        if (request.BloodType != null)
            patient.BloodType = request.BloodType;
        if (request.Notes != null)
            patient.Notes = request.Notes;
            
        patient.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        
        return Ok(Map(patient));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var patient = await _db.Patients
            .Include(p => p.ClinicalHistories)
            .FirstOrDefaultAsync(p => p.Id == id);
        
        if (patient == null)
        {
            return NotFound();
        }
        
        // Verificar permisos
        var isOdontologo = User.IsInRole(Roles.Odontologo);
        if (isOdontologo && patient.OdontologoId != userId)
        {
            return Forbid();
        }
        
        // Verificar si tiene historias clínicas
        if (patient.ClinicalHistories.Any())
        {
            return BadRequest(new { message = "No se puede eliminar un paciente con historias clínicas asociadas" });
        }
        
        _db.Patients.Remove(patient);
        await _db.SaveChangesAsync();
        
        return NoContent();
    }

    private Guid GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new UnauthorizedAccessException();
        }
        return Guid.Parse(id);
    }

    private static PatientDto Map(Patient patient)
    {
        return new PatientDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            IdNumber = patient.IdNumber,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Address = patient.Address,
            Phone = patient.Phone,
            Email = patient.Email,
            EmergencyContact = patient.EmergencyContact,
            EmergencyPhone = patient.EmergencyPhone,
            Allergies = patient.Allergies,
            Medications = patient.Medications,
            Diseases = patient.Diseases,
            BloodType = patient.BloodType,
            Notes = patient.Notes,
            CreatedAt = patient.CreatedAt,
            UpdatedAt = patient.UpdatedAt
        };
    }
}
