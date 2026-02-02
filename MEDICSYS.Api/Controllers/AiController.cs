using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MEDICSYS.Api.Contracts;

namespace MEDICSYS.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    [Authorize]
    [HttpPost("suggest-notes")]
    public ActionResult<AiSuggestResponse> SuggestNotes(AiSuggestRequest request)
    {
        var summary = BuildSuggestion(request);
        return Ok(new AiSuggestResponse { Suggestion = summary });
    }

    private static string BuildSuggestion(AiSuggestRequest request)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            parts.Add($"Motivo: {request.Reason}.");
        }
        if (!string.IsNullOrWhiteSpace(request.CurrentIssue))
        {
            parts.Add($"Problema actual: {request.CurrentIssue}.");
        }
        if (!string.IsNullOrWhiteSpace(request.Plan))
        {
            parts.Add($"Plan: {request.Plan}.");
        }
        if (!string.IsNullOrWhiteSpace(request.Procedures))
        {
            parts.Add($"Procedimientos: {request.Procedures}.");
        }
        if (parts.Count == 0 && !string.IsNullOrWhiteSpace(request.Notes))
        {
            parts.Add(request.Notes);
        }
        if (parts.Count == 0)
        {
            parts.Add("Sin observaciones relevantes. Se recomienda control y seguimiento.");
        }
        return string.Join(" ", parts);
    }
}

public class AiSuggestResponse
{
    public string Suggestion { get; set; } = string.Empty;
}
