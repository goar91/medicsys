using System.Text.Json.Nodes;

namespace MEDICSYS.Api.Models.Odontologia;

public class OdontologoClinicalHistory
{
    public Guid Id { get; set; }
    public Guid OdontologoId { get; set; }
    public string PatientName { get; set; } = null!;
    public string PatientIdNumber { get; set; } = null!;
    public JsonObject Data { get; set; } = new();
    public ClinicalHistoryStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ApplicationUser Odontologo { get; set; } = null!;
}
