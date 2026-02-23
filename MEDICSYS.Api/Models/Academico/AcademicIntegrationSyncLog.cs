namespace MEDICSYS.Api.Models.Academico;

public class AcademicIntegrationSyncLog
{
    public Guid Id { get; set; }
    public Guid IntegrationId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public IntegrationSyncStatus Status { get; set; }
    public int RecordsProcessed { get; set; }
    public string? Message { get; set; }

    public AcademicIntegrationConnector Integration { get; set; } = null!;
}

public enum IntegrationSyncStatus
{
    Success,
    Failed,
    Partial
}
