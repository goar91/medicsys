namespace MEDICSYS.Api.Models.Academico;

public class AcademicAccreditationCriterion
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public AccreditationCriterionStatus Status { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ICollection<AcademicAccreditationEvidence> Evidences { get; set; } = new List<AcademicAccreditationEvidence>();
    public ICollection<AcademicImprovementPlanAction> ImprovementActions { get; set; } = new List<AcademicImprovementPlanAction>();
}

public enum AccreditationCriterionStatus
{
    NotStarted,
    InProgress,
    Compliant,
    NeedsImprovement
}
