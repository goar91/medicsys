namespace MEDICSYS.Api.Models.Odontologia;

public enum InsuranceClaimStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    NeedsInformation = 3
}

public class InsuranceClaim
{
    public Guid Id { get; set; }
    public Guid OdontologoId { get; set; }
    public Guid PatientId { get; set; }
    public string InsurerName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string ProcedureCode { get; set; } = string.Empty;
    public string ProcedureDescription { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public InsuranceClaimStatus Status { get; set; } = InsuranceClaimStatus.Pending;
    public string? ResponseMessage { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
