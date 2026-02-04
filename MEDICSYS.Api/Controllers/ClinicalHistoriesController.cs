using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Contracts;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers;

[ApiController]
[Route("api/clinical-histories")]
[Authorize(Roles = $"{Roles.Student},{Roles.Professor},{Roles.Odontologo}")]
public class ClinicalHistoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public ClinicalHistoriesController(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClinicalHistoryDto>>> GetAll()
    {
        var userId = await ResolveUserIdAsync();
        var query = _db.ClinicalHistories
            .Include(x => x.Student)
            .AsNoTracking();

        if (!IsReviewer())
        {
            query = query.Where(x => x.StudentId == userId);
        }

        var histories = await query
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        return Ok(histories.Select(Map));
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClinicalHistoryDto>> GetById(Guid id)
    {
        var userId = await ResolveUserIdAsync();
        var history = await _db.ClinicalHistories
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (history == null)
        {
            return NotFound();
        }

        if (!IsReviewer() && history.StudentId != userId)
        {
            return Forbid();
        }

        return Ok(Map(history));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ClinicalHistoryDto>> Create(ClinicalHistoryUpsertRequest request)
    {
        var userId = await ResolveUserIdAsync();
        var json = JsonSerializer.Serialize(request.Data);

        var history = new ClinicalHistory
        {
            Id = Guid.NewGuid(),
            StudentId = userId,
            PatientId = request.PatientId,
            Data = json,
            Status = ClinicalHistoryStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ClinicalHistories.Add(history);
        await _db.SaveChangesAsync();

        history.Student = await _db.Users.FirstAsync(u => u.Id == userId);
        return CreatedAtAction(nameof(GetById), new { id = history.Id }, Map(history));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClinicalHistoryDto>> Update(Guid id, ClinicalHistoryUpsertRequest request)
    {
        var userId = await ResolveUserIdAsync();
        var history = await _db.ClinicalHistories
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (history == null)
        {
            return NotFound();
        }

        var isProfessor = IsReviewer();
        if (!isProfessor && history.StudentId != userId)
        {
            return Forbid();
        }

        if (!isProfessor && (history.Status == ClinicalHistoryStatus.Submitted || history.Status == ClinicalHistoryStatus.Approved))
        {
            return BadRequest("Clinical history cannot be edited after submission.");
        }

        history.Data = JsonSerializer.Serialize(request.Data);
        history.PatientId = request.PatientId;
        history.UpdatedAt = DateTime.UtcNow;

        if (!isProfessor && history.Status == ClinicalHistoryStatus.Rejected)
        {
            history.Status = ClinicalHistoryStatus.Draft;
            history.ReviewedAt = null;
            history.ReviewedById = null;
            history.ReviewNotes = null;
        }

        await _db.SaveChangesAsync();

        return Ok(Map(history));
    }

    [Authorize(Roles = $"{Roles.Professor},{Roles.Odontologo}")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var userId = await ResolveUserIdAsync();
        var history = await _db.ClinicalHistories
            .FirstOrDefaultAsync(x => x.Id == id);

        if (history == null)
        {
            return NotFound();
        }

        // Verificar que sea el creador o profesor
        var isProfessor = IsReviewer();
        if (!isProfessor && history.StudentId != userId)
        {
            return Forbid();
        }

        _db.ClinicalHistories.Remove(history);
        await _db.SaveChangesAsync();

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var uploadsRoot = Path.Combine(webRoot, "uploads", id.ToString());
        if (Directory.Exists(uploadsRoot))
        {
            Directory.Delete(uploadsRoot, true);
        }

        return NoContent();
    }

    [Authorize(Roles = Roles.Student)]
    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ClinicalHistoryDto>> Submit(Guid id)
    {
        var userId = await ResolveUserIdAsync();
        var history = await _db.ClinicalHistories
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (history == null)
        {
            return NotFound();
        }

        if (history.StudentId != userId)
        {
            return Forbid();
        }

        if (history.Status != ClinicalHistoryStatus.Draft)
        {
            return BadRequest("Clinical history must be in draft status to submit.");
        }

        history.Status = ClinicalHistoryStatus.Submitted;
        history.SubmittedAt = DateTime.UtcNow;
        history.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(Map(history));
    }

    [Authorize(Roles = Roles.Student)]
    [HttpPost("{id:guid}/media")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<ClinicalHistoryDto>> UploadMedia(Guid id, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required.");
        }

        var userId = await ResolveUserIdAsync();
        var history = await _db.ClinicalHistories
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (history == null)
        {
            return NotFound();
        }

        if (history.StudentId != userId)
        {
            return Forbid();
        }

        if (history.Status == ClinicalHistoryStatus.Submitted || history.Status == ClinicalHistoryStatus.Approved)
        {
            return BadRequest("Clinical history cannot be edited after submission.");
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) &&
            !contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only image or video files are allowed.");
        }

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var uploadsRoot = Path.Combine(webRoot, "uploads", id.ToString());
        Directory.CreateDirectory(uploadsRoot);

        var extension = Path.GetExtension(file.FileName);
        var fileId = Guid.NewGuid();
        var safeName = $"{fileId}{extension}";
        var filePath = Path.Combine(uploadsRoot, safeName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"{Request.Scheme}://{Request.Host}/uploads/{id}/{safeName}";
        var root = JsonNode.Parse(history.Data) as JsonObject ?? new JsonObject();
        var medios = root["medios"] as JsonObject ?? new JsonObject();
        var assets = medios["assets"] as JsonArray ?? new JsonArray();

        assets.Add(new JsonObject
        {
            ["id"] = fileId.ToString(),
            ["fileName"] = file.FileName,
            ["url"] = url,
            ["contentType"] = contentType,
            ["uploadedAt"] = DateTime.UtcNow
        });

        medios["assets"] = assets;
        root["medios"] = medios;
        history.Data = root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        history.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(Map(history));
    }

    [Authorize(Roles = Roles.Professor)]
    [HttpPost("{id:guid}/review")]
    public async Task<ActionResult<ClinicalHistoryDto>> Review(Guid id, ClinicalHistoryReviewRequest request)
    {
        var userId = await ResolveUserIdAsync();
        var history = await _db.ClinicalHistories
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (history == null)
        {
            return NotFound();
        }

        if (history.Status != ClinicalHistoryStatus.Submitted)
        {
            return BadRequest("Clinical history must be submitted before review.");
        }

        history.Status = request.Approved ? ClinicalHistoryStatus.Approved : ClinicalHistoryStatus.Rejected;
        history.ReviewedAt = DateTime.UtcNow;
        history.ReviewedById = userId;
        history.ReviewNotes = request.Notes;
        history.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(Map(history));
    }

    private bool IsReviewer()
    {
        return User.IsInRole(Roles.Professor) || User.IsInRole(Roles.Odontologo);
    }

    private async Task<Guid> ResolveUserIdAsync()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(id, out var userId))
        {
            var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == userId);
            if (exists)
            {
                return userId;
            }
        }

        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name);
        if (!string.IsNullOrWhiteSpace(email))
        {
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email || u.UserName == email);
            if (user != null)
            {
                return user.Id;
            }
        }

        throw new UnauthorizedAccessException();
    }

    private static ClinicalHistoryDto Map(ClinicalHistory history)
    {
        using var doc = JsonDocument.Parse(history.Data);
        return new ClinicalHistoryDto
        {
            Id = history.Id,
            StudentId = history.StudentId,
            StudentName = history.Student?.FullName ?? string.Empty,
            Status = history.Status,
            Data = doc.RootElement.Clone(),
            CreatedAt = history.CreatedAt,
            UpdatedAt = history.UpdatedAt,
            SubmittedAt = history.SubmittedAt,
            ReviewedAt = history.ReviewedAt,
            ReviewNotes = history.ReviewNotes
        };
    }
}
