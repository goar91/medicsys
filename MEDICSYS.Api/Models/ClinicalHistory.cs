namespace MEDICSYS.Api.Models;

public class ClinicalHistory
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public ApplicationUser Student { get; set; } = null!;
    public Guid? PatientId { get; set; }
    public Patient? Patient { get; set; }
    public ClinicalHistoryStatus Status { get; set; } = ClinicalHistoryStatus.Draft;
    public string Data { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public Guid? ReviewedById { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}
