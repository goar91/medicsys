namespace MEDICSYS.Api.Models.Academico;

public class AcademicStudentRiskFlag
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public StudentRiskLevel RiskLevel { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }

    public ApplicationUser Student { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ApplicationUser? ResolvedByUser { get; set; }
}

public enum StudentRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
