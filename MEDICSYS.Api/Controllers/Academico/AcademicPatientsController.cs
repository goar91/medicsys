using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models.Academico;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers.Academico;

[ApiController]
[Route("api/academic/patients")]
[Authorize(Roles = $"{Roles.Professor}")]
public class AcademicPatientsController : ControllerBase
{
    private readonly AcademicDbContext _db;

    public AcademicPatientsController(AcademicDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AcademicPatientDto>>> GetAll([FromQuery] string? search)
    {
        var query = _db.AcademicPatients
            .Include(p => p.CreatedByProfessor)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(searchLower) ||
                p.LastName.ToLower().Contains(searchLower) ||
                p.IdNumber.Contains(search));
        }

        var patients = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        return Ok(patients.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AcademicPatientDto>> GetById(Guid id)
    {
        var patient = await _db.AcademicPatients
            .Include(p => p.CreatedByProfessor)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null)
            return NotFound();

        return Ok(MapToDto(patient));
    }

    [HttpPost]
    public async Task<ActionResult<AcademicPatientDto>> Create([FromBody] CreateAcademicPatientRequest request)
    {
        var professorId = GetUserId();

        // Verificar si ya existe un paciente con esa cédula
        if (await _db.AcademicPatients.AnyAsync(p => p.IdNumber == request.IdNumber))
        {
            return BadRequest(new { message = "Ya existe un paciente con esa cédula." });
        }

        var patient = new AcademicPatient
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            IdNumber = request.IdNumber,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            BloodType = request.BloodType,
            Allergies = request.Allergies,
            MedicalConditions = request.MedicalConditions,
            EmergencyContact = request.EmergencyContact,
            EmergencyPhone = request.EmergencyPhone,
            CreatedByProfessorId = professorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.AcademicPatients.Add(patient);
        await _db.SaveChangesAsync();

        patient = await _db.AcademicPatients
            .Include(p => p.CreatedByProfessor)
            .FirstAsync(p => p.Id == patient.Id);

        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, MapToDto(patient));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AcademicPatientDto>> Update(Guid id, [FromBody] UpdateAcademicPatientRequest request)
    {
        var patient = await _db.AcademicPatients.FindAsync(id);
        if (patient == null)
            return NotFound();

        // Verificar si otro paciente ya tiene esa cédula
        if (await _db.AcademicPatients.AnyAsync(p => p.IdNumber == request.IdNumber && p.Id != id))
        {
            return BadRequest(new { message = "Ya existe otro paciente con esa cédula." });
        }

        patient.FirstName = request.FirstName;
        patient.LastName = request.LastName;
        patient.IdNumber = request.IdNumber;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender;
        patient.Phone = request.Phone;
        patient.Email = request.Email;
        patient.Address = request.Address;
        patient.BloodType = request.BloodType;
        patient.Allergies = request.Allergies;
        patient.MedicalConditions = request.MedicalConditions;
        patient.EmergencyContact = request.EmergencyContact;
        patient.EmergencyPhone = request.EmergencyPhone;
        patient.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        patient = await _db.AcademicPatients
            .Include(p => p.CreatedByProfessor)
            .FirstAsync(p => p.Id == id);

        return Ok(MapToDto(patient));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var patient = await _db.AcademicPatients.FindAsync(id);
        if (patient == null)
            return NotFound();

        _db.AcademicPatients.Remove(patient);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static AcademicPatientDto MapToDto(AcademicPatient patient)
    {
        return new AcademicPatientDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            FullName = $"{patient.FirstName} {patient.LastName}",
            IdNumber = patient.IdNumber,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Phone = patient.Phone,
            Email = patient.Email,
            Address = patient.Address,
            BloodType = patient.BloodType,
            Allergies = patient.Allergies,
            MedicalConditions = patient.MedicalConditions,
            EmergencyContact = patient.EmergencyContact,
            EmergencyPhone = patient.EmergencyPhone,
            CreatedByProfessorName = patient.CreatedByProfessor?.FullName ?? "",
            CreatedAt = patient.CreatedAt,
            UpdatedAt = patient.UpdatedAt
        };
    }
}

public record AcademicPatientDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string IdNumber { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? BloodType { get; init; }
    public string? Allergies { get; init; }
    public string? MedicalConditions { get; init; }
    public string? EmergencyContact { get; init; }
    public string? EmergencyPhone { get; init; }
    public string CreatedByProfessorName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateAcademicPatientRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string IdNumber { get; init; }
    public DateTime DateOfBirth { get; init; }
    public required string Gender { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? BloodType { get; init; }
    public string? Allergies { get; init; }
    public string? MedicalConditions { get; init; }
    public string? EmergencyContact { get; init; }
    public string? EmergencyPhone { get; init; }
}

public record UpdateAcademicPatientRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string IdNumber { get; init; }
    public DateTime DateOfBirth { get; init; }
    public required string Gender { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? BloodType { get; init; }
    public string? Allergies { get; init; }
    public string? MedicalConditions { get; init; }
    public string? EmergencyContact { get; init; }
    public string? EmergencyPhone { get; init; }
}
