namespace MEDICSYS.Api.Models.Odontologia;

public enum TelemedicineSessionStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public class TelemedicineSession
{
    public Guid Id { get; set; }
    public Guid OdontologoId { get; set; }
    public Guid? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? MeetingLink { get; set; }
    public DateTime ScheduledStartAt { get; set; }
    public DateTime ScheduledEndAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public TelemedicineSessionStatus Status { get; set; } = TelemedicineSessionStatus.Scheduled;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<TelemedicineMessage> Messages { get; set; } = new();
}

public class TelemedicineMessage
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public TelemedicineSession Session { get; set; } = null!;
    public string SenderRole { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
