namespace MEDICSYS.Api.Models.Academico;

public class AcademicImprovementPlanAction
{
    public Guid Id { get; set; }
    public Guid CriterionId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Responsible { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal ProgressPercent { get; set; }
    public ImprovementActionStatus Status { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public AcademicAccreditationCriterion Criterion { get; set; } = null!;
    public ApplicationUser CreatedByUser { get; set; } = null!;
}

public enum ImprovementActionStatus
{
    Planned,
    InProgress,
    Completed,
    Overdue
}
