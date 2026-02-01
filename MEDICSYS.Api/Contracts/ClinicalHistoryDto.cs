using System.Text.Json;
using MEDICSYS.Api.Models;

namespace MEDICSYS.Api.Contracts;

public class ClinicalHistoryDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public ClinicalHistoryStatus Status { get; set; }
    public JsonElement Data { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}
