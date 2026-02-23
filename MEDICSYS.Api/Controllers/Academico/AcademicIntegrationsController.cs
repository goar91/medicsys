using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models.Academico;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Controllers.Academico;

[ApiController]
[Route("api/academic/integrations")]
[Authorize(Roles = Roles.Admin)]
public class AcademicIntegrationsController : ControllerBase
{
    private readonly AcademicDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public AcademicIntegrationsController(AcademicDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<IntegrationConnectorDto>>> GetAll()
    {
        var items = await _db.AcademicIntegrationConnectors
            .AsNoTracking()
            .OrderBy(x => x.ProviderType)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Ok(items.Select(MapConnector));
    }

    [HttpGet("{id:guid}/logs")]
    public async Task<ActionResult<IEnumerable<IntegrationSyncLogDto>>> GetLogs(Guid id, [FromQuery] int take = 100)
    {
        var exists = await _db.AcademicIntegrationConnectors.AnyAsync(i => i.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        var normalizedTake = Math.Clamp(take, 1, 500);
        var logs = await _db.AcademicIntegrationSyncLogs
            .AsNoTracking()
            .Where(l => l.IntegrationId == id)
            .OrderByDescending(l => l.StartedAt)
            .Take(normalizedTake)
            .ToListAsync();

        return Ok(logs.Select(MapSyncLog));
    }

    [HttpPost]
    public async Task<ActionResult<IntegrationConnectorDto>> Create([FromBody] UpsertIntegrationConnectorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name es obligatorio." });
        }

        var normalizedName = request.Name.Trim();
        var duplicate = await _db.AcademicIntegrationConnectors.AnyAsync(i => i.Name == normalizedName);
        if (duplicate)
        {
            return Conflict(new { message = "Ya existe una integración con ese nombre." });
        }

        var now = DateTimeHelper.Now();
        var item = new AcademicIntegrationConnector
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            ProviderType = request.ProviderType,
            EndpointUrl = string.IsNullOrWhiteSpace(request.EndpointUrl) ? null : request.EndpointUrl.Trim(),
            ApiKeyHint = string.IsNullOrWhiteSpace(request.ApiKeyHint) ? null : request.ApiKeyHint.Trim(),
            Enabled = request.Enabled,
            CreatedByUserId = GetUserId(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.AcademicIntegrationConnectors.Add(item);
        await _db.SaveChangesAsync();

        return Ok(MapConnector(item));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<IntegrationConnectorDto>> Update(Guid id, [FromBody] UpsertIntegrationConnectorRequest request)
    {
        var item = await _db.AcademicIntegrationConnectors.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var normalizedName = request.Name.Trim();
            var duplicate = await _db.AcademicIntegrationConnectors
                .AnyAsync(i => i.Id != id && i.Name == normalizedName);
            if (duplicate)
            {
                return Conflict(new { message = "Ya existe otra integración con ese nombre." });
            }
            item.Name = normalizedName;
        }

        item.ProviderType = request.ProviderType;
        item.EndpointUrl = string.IsNullOrWhiteSpace(request.EndpointUrl) ? null : request.EndpointUrl.Trim();
        item.ApiKeyHint = string.IsNullOrWhiteSpace(request.ApiKeyHint) ? null : request.ApiKeyHint.Trim();
        item.Enabled = request.Enabled;
        item.UpdatedAt = DateTimeHelper.Now();

        await _db.SaveChangesAsync();
        return Ok(MapConnector(item));
    }

    [HttpPost("{id:guid}/test")]
    public async Task<ActionResult<IntegrationTestResultDto>> Test(Guid id)
    {
        var integration = await _db.AcademicIntegrationConnectors.FirstOrDefaultAsync(i => i.Id == id);
        if (integration == null)
        {
            return NotFound();
        }

        if (!integration.Enabled)
        {
            return BadRequest(new { message = "La integración está deshabilitada." });
        }

        var result = await RunIntegrationCallAsync(integration, isManualSync: false);
        return Ok(result);
    }

    [HttpPost("{id:guid}/sync")]
    public async Task<ActionResult<IntegrationTestResultDto>> Sync(Guid id)
    {
        var integration = await _db.AcademicIntegrationConnectors.FirstOrDefaultAsync(i => i.Id == id);
        if (integration == null)
        {
            return NotFound();
        }

        if (!integration.Enabled)
        {
            return BadRequest(new { message = "La integración está deshabilitada." });
        }

        var result = await RunIntegrationCallAsync(integration, isManualSync: true);
        return Ok(result);
    }

    private async Task<IntegrationTestResultDto> RunIntegrationCallAsync(AcademicIntegrationConnector integration, bool isManualSync)
    {
        var startedAt = DateTimeHelper.Now();
        var log = new AcademicIntegrationSyncLog
        {
            Id = Guid.NewGuid(),
            IntegrationId = integration.Id,
            StartedAt = startedAt,
            Status = IntegrationSyncStatus.Failed,
            RecordsProcessed = 0
        };

        _db.AcademicIntegrationSyncLogs.Add(log);
        await _db.SaveChangesAsync();

        try
        {
            if (string.IsNullOrWhiteSpace(integration.EndpointUrl))
            {
                throw new InvalidOperationException("La integración no tiene EndpointUrl configurada.");
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            var payload = new
            {
                integration = integration.Name,
                provider = integration.ProviderType.ToString(),
                mode = isManualSync ? "manual-sync" : "connectivity-test",
                emittedAt = DateTime.UtcNow,
                source = "MEDICSYS Academic"
            };
            var requestBody = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, integration.EndpointUrl)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(integration.ApiKeyHint))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", integration.ApiKeyHint);
            }

            var response = await client.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            var ok = response.IsSuccessStatusCode;
            var now = DateTimeHelper.Now();
            integration.LastSyncAt = now;
            integration.LastStatus = ok ? "Success" : "Failed";
            integration.LastMessage = $"HTTP {(int)response.StatusCode}: {Truncate(responseText, 500)}";
            integration.UpdatedAt = now;

            log.EndedAt = now;
            log.Status = ok ? IntegrationSyncStatus.Success : IntegrationSyncStatus.Failed;
            log.RecordsProcessed = ok ? 1 : 0;
            log.Message = integration.LastMessage;

            await _db.SaveChangesAsync();

            return new IntegrationTestResultDto(
                integration.Id,
                integration.Name,
                integration.ProviderType.ToString(),
                ok,
                integration.LastMessage,
                now,
                log.Status.ToString());
        }
        catch (Exception ex)
        {
            var now = DateTimeHelper.Now();
            integration.LastSyncAt = now;
            integration.LastStatus = "Failed";
            integration.LastMessage = Truncate(ex.Message, 500);
            integration.UpdatedAt = now;

            log.EndedAt = now;
            log.Status = IntegrationSyncStatus.Failed;
            log.Message = integration.LastMessage;

            await _db.SaveChangesAsync();

            return new IntegrationTestResultDto(
                integration.Id,
                integration.Name,
                integration.ProviderType.ToString(),
                false,
                integration.LastMessage,
                now,
                log.Status.ToString());
        }
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value.Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static IntegrationConnectorDto MapConnector(AcademicIntegrationConnector item) => new(
        item.Id,
        item.Name,
        item.ProviderType.ToString(),
        item.EndpointUrl,
        item.ApiKeyHint,
        item.Enabled,
        item.LastSyncAt,
        item.LastStatus,
        item.LastMessage,
        item.CreatedAt,
        item.UpdatedAt);

    private static IntegrationSyncLogDto MapSyncLog(AcademicIntegrationSyncLog item) => new(
        item.Id,
        item.IntegrationId,
        item.StartedAt,
        item.EndedAt,
        item.Status.ToString(),
        item.RecordsProcessed,
        item.Message);
}

public record IntegrationConnectorDto(
    Guid Id,
    string Name,
    string ProviderType,
    string? EndpointUrl,
    string? ApiKeyHint,
    bool Enabled,
    DateTime? LastSyncAt,
    string? LastStatus,
    string? LastMessage,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record IntegrationSyncLogDto(
    Guid Id,
    Guid IntegrationId,
    DateTime StartedAt,
    DateTime? EndedAt,
    string Status,
    int RecordsProcessed,
    string? Message);

public record UpsertIntegrationConnectorRequest(
    string? Name,
    IntegrationProviderType ProviderType,
    string? EndpointUrl,
    string? ApiKeyHint,
    bool Enabled);

public record IntegrationTestResultDto(
    Guid IntegrationId,
    string IntegrationName,
    string ProviderType,
    bool Success,
    string? Message,
    DateTime ProcessedAt,
    string SyncStatus);
