namespace MEDICSYS.Api.Contracts;

public class AvailabilityResponse
{
    public DateTime Date { get; set; }
    public string TimeZone { get; set; } = "local";
    public List<TimeSlotDto> Slots { get; set; } = new();
}

public class TimeSlotDto
{
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsAvailable { get; set; }
}
