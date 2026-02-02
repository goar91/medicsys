namespace MEDICSYS.Api.Contracts;

public class AiSuggestRequest
{
    public string? Reason { get; set; }
    public string? CurrentIssue { get; set; }
    public string? Notes { get; set; }
    public string? Plan { get; set; }
    public string? Procedures { get; set; }
}
