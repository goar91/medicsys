namespace MEDICSYS.Api.Models.Academico;

public class AcademicDataAuditEvent
{
    public Guid Id { get; set; }
    public DataAuditEventType EventType { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public string? SubjectType { get; set; }
    public string? SubjectIdentifier { get; set; }
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAt { get; set; }
}

public enum DataAuditEventType
{
    Read,
    Create,
    Update,
    Delete,
    Export,
    Login,
    Other
}
