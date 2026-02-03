namespace MEDICSYS.Api.Models;

public class Patient
{
    public Guid Id { get; set; }
    public Guid OdontologoId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Allergies { get; set; }
    public string? Medications { get; set; }
    public string? Diseases { get; set; }
    public string? BloodType { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navegación
    public ApplicationUser Odontologo { get; set; } = null!;
    public ICollection<ClinicalHistory> ClinicalHistories { get; set; } = new List<ClinicalHistory>();
}
