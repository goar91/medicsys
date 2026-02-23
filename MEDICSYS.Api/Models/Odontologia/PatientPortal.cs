namespace MEDICSYS.Api.Models.Odontologia;

public enum PatientPortalNotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}

public class PatientPortalPreference
{
    public Guid Id { get; set; }
    public Guid OdontologoId { get; set; }
    public Guid PatientId { get; set; }
    public bool EmailEnabled { get; set; } = true;
    public bool WhatsAppEnabled { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PatientPortalNotification
{
    public Guid Id { get; set; }
    public Guid OdontologoId { get; set; }
    public Guid PatientId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime ScheduledFor { get; set; }
    public DateTime? SentAt { get; set; }
    public PatientPortalNotificationStatus Status { get; set; } = PatientPortalNotificationStatus.Pending;
    public string? ExternalReference { get; set; }
}
