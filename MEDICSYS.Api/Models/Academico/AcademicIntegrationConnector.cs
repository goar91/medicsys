namespace MEDICSYS.Api.Models.Academico;

public class AcademicIntegrationConnector
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IntegrationProviderType ProviderType { get; set; }
    public string? EndpointUrl { get; set; }
    public string? ApiKeyHint { get; set; }
    public bool Enabled { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? LastStatus { get; set; }
    public string? LastMessage { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser CreatedByUser { get; set; } = null!;
    public ICollection<AcademicIntegrationSyncLog> SyncLogs { get; set; } = new List<AcademicIntegrationSyncLog>();
}

public enum IntegrationProviderType
{
    Moodle,
    Canvas,
    Erp,
    Email,
    WhatsApp,
    Sniese,
    Siies,
    Webhook
}
