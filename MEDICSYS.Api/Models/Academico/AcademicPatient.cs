namespace MEDICSYS.Api.Models.Academico;

public class AcademicPatient
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string IdNumber { get; set; } // Cédula
    public DateTime DateOfBirth { get; set; }
    public required string Gender { get; set; } // M, F, Other
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
    public string? MedicalConditions { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedByProfessorId { get; set; }
    public ApplicationUser? CreatedByProfessor { get; set; }
}
