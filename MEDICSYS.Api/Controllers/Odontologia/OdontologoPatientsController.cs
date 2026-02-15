using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models.Odontologia;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers.Odontologia;

[ApiController]
[Route("api/odontologia/patients")]
[Authorize(Roles = Roles.Odontologo)]
public class OdontologoPatientsController : ControllerBase
{
    private readonly OdontologoDbContext _db;

    public OdontologoPatientsController(OdontologoDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OdontologoPatient>>> GetAll()
    {
        var odontologoId = GetUserId();
        
        var patients = await _db.OdontologoPatients
            .Where(p => p.OdontologoId == odontologoId)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();
            
        return Ok(patients);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OdontologoPatient>> GetById(Guid id)
    {
        var odontologoId = GetUserId();
        
        var patient = await _db.OdontologoPatients
            .FirstOrDefaultAsync(p => p.Id == id && p.OdontologoId == odontologoId);

        if (patient == null)
            return NotFound();

        return Ok(patient);
    }

    [HttpPost]
    public async Task<ActionResult<OdontologoPatient>> Create([FromBody] CreateOdontologoPatientRequest request)
    {
        var odontologoId = GetUserId();

        var exists = await _db.OdontologoPatients
            .AnyAsync(p => p.IdNumber == request.IdNumber && p.OdontologoId == odontologoId);

        if (exists)
            return BadRequest(new { message = "Ya existe un paciente con esta cédula" });

        var patient = new OdontologoPatient
        {
            Id = Guid.NewGuid(),
            OdontologoId = odontologoId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IdNumber = request.IdNumber,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.OdontologoPatients.Add(patient);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OdontologoPatient>> Update(Guid id, [FromBody] UpdateOdontologoPatientRequest request)
    {
        var odontologoId = GetUserId();
        
        var patient = await _db.OdontologoPatients
            .FirstOrDefaultAsync(p => p.Id == id && p.OdontologoId == odontologoId);

        if (patient == null)
            return NotFound();

        patient.FirstName = request.FirstName;
        patient.LastName = request.LastName;
        patient.Phone = request.Phone;
        patient.Email = request.Email;
        patient.Address = request.Address;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender;
        patient.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(patient);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var odontologoId = GetUserId();
        
        var patient = await _db.OdontologoPatients
            .FirstOrDefaultAsync(p => p.Id == id && p.OdontologoId == odontologoId);

        if (patient == null)
            return NotFound();

        _db.OdontologoPatients.Remove(patient);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateOdontologoPatientRequest(
    string FirstName,
    string LastName,
    string IdNumber,
    string DateOfBirth,
    string Gender,
    string Address,
    string Phone,
    string Email
);

public record UpdateOdontologoPatientRequest(
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    string Address,
    string DateOfBirth,
    string Gender
);
