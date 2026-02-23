namespace MEDICSYS.Api.Models.Academico;

public class AcademicDataAnonymizationRequest
{
    public Guid Id { get; set; }
    public string SubjectType { get; set; } = string.Empty;
    public Guid? SubjectId { get; set; }
    public string SubjectIdentifier { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DataAnonymizationRequestStatus Status { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResolutionNotes { get; set; }

    public ApplicationUser RequestedByUser { get; set; } = null!;
    public ApplicationUser? ReviewedByUser { get; set; }
}

public enum DataAnonymizationRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Completed
}
