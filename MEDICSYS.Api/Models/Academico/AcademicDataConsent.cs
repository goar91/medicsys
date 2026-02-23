namespace MEDICSYS.Api.Models.Academico;

public class AcademicDataConsent
{
    public Guid Id { get; set; }
    public string SubjectType { get; set; } = string.Empty;
    public Guid? SubjectId { get; set; }
    public string SubjectIdentifier { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string LegalBasis { get; set; } = string.Empty;
    public bool Granted { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid CollectedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser CollectedByUser { get; set; } = null!;
}
