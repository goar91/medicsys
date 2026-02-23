namespace MEDICSYS.Api.Models.Academico;

public class AcademicAccreditationEvidence
{
    public Guid Id { get; set; }
    public Guid CriterionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SourceType { get; set; } = "Document";
    public string? EvidenceUrl { get; set; }
    public bool IsVerified { get; set; }
    public Guid UploadedByUserId { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public AcademicAccreditationCriterion Criterion { get; set; } = null!;
    public ApplicationUser UploadedByUser { get; set; } = null!;
    public ApplicationUser? VerifiedByUser { get; set; }
}
