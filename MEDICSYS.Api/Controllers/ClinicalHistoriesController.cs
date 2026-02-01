using System.Security.Claims;
using System.Text.Json;
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
public class ClinicalHistoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClinicalHistoriesController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClinicalHistoryDto>>> GetAll()
    {
        var userId = GetUserId();
        var query = _db.ClinicalHistories
            .Include(x => x.Student)
            .AsNoTracking();

        if (!IsProfessor())
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
        var userId = GetUserId();
        var history = await _db.ClinicalHistories
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (history == null)
        {
            return NotFound();
        }

        if (!IsProfessor() && history.StudentId != userId)
        {
            return Forbid();
        }

        return Ok(Map(history));
    }

    [Authorize(Roles = Roles.Student)]
    [HttpPost]
    public async Task<ActionResult<ClinicalHistoryDto>> Create(ClinicalHistoryUpsertRequest request)
    {
        var userId = GetUserId();
        var json = JsonSerializer.Serialize(request.Data);

        var history = new ClinicalHistory
        {
            Id = Guid.NewGuid(),
            StudentId = userId,
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

    [Authorize(Roles = Roles.Student)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClinicalHistoryDto>> Update(Guid id, ClinicalHistoryUpsertRequest request)
    {
        var userId = GetUserId();
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

        history.Data = JsonSerializer.Serialize(request.Data);
        history.UpdatedAt = DateTime.UtcNow;

        if (history.Status == ClinicalHistoryStatus.Rejected)
        {
            history.Status = ClinicalHistoryStatus.Draft;
            history.ReviewedAt = null;
            history.ReviewedById = null;
            history.ReviewNotes = null;
        }

        await _db.SaveChangesAsync();

        return Ok(Map(history));
    }

    [Authorize(Roles = Roles.Student)]
    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ClinicalHistoryDto>> Submit(Guid id)
    {
        var userId = GetUserId();
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

    [Authorize(Roles = Roles.Professor)]
    [HttpPost("{id:guid}/review")]
    public async Task<ActionResult<ClinicalHistoryDto>> Review(Guid id, ClinicalHistoryReviewRequest request)
    {
        var userId = GetUserId();
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

    private bool IsProfessor()
    {
        return User.IsInRole(Roles.Professor);
    }

    private Guid GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new UnauthorizedAccessException();
        }

        return Guid.Parse(id);
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
