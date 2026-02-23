namespace MEDICSYS.Api.Models.Odontologia;

public class SignedClinicalDocument
{
    public Guid Id { get; set; }
    public Guid OdontologoId { get; set; }
    public Guid PatientId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentHash { get; set; } = string.Empty;
    public string SignatureProvider { get; set; } = string.Empty;
    public string? SignatureSerial { get; set; }
    public DateTime SignedAt { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string? Notes { get; set; }
}
