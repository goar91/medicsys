namespace MEDICSYS.Api.Models.Academico;

public class AcademicSupervisionAssignment
{
    public Guid Id { get; set; }
    public Guid ProfessorId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? PatientId { get; set; }
    public Guid AssignedByUserId { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser Professor { get; set; } = null!;
    public ApplicationUser Student { get; set; } = null!;
    public AcademicPatient? Patient { get; set; }
    public ApplicationUser AssignedByUser { get; set; } = null!;
}
