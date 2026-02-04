using System.Text.Json.Nodes;

namespace MEDICSYS.Api.Models.Academico;

public class AcademicClinicalHistory
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid? ReviewedByProfessorId { get; set; }
    public JsonObject Data { get; set; } = new();
    public ClinicalHistoryStatus Status { get; set; }
    public string? ProfessorComments { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ApplicationUser Student { get; set; } = null!;
    public ApplicationUser? ReviewedByProfessor { get; set; }
}
