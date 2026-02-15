using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Contracts;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers;

[ApiController]
[Route("api/accounting")]
[Authorize(Roles = Roles.Odontologo)]
public class AccountingController : ControllerBase
{
    private readonly OdontologoDbContext _db;

    public AccountingController(OdontologoDbContext db)
    {
        _db = db;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<AccountingCategoryDto>>> GetCategories()
    {
        var categories = await _db.AccountingCategories
            .AsNoTracking()
            .OrderBy(x => x.Group)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Ok(categories.Select(MapCategory));
    }

    [HttpPost("categories")]
    public async Task<ActionResult<AccountingCategoryDto>> CreateCategory(AccountingCategoryRequest request)
    {
        if (!Enum.TryParse<AccountingEntryType>(request.Type, true, out var categoryType))
        {
            return BadRequest("Tipo de categoria invalido.");
        }

        var name = request.Name.Trim();
        var group = request.Group.Trim();
        var existing = await _db.AccountingCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == name && x.Type == categoryType);

        if (existing != null)
        {
            return Ok(MapCategory(existing));
        }

        var category = new AccountingCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            Group = group,
            Type = categoryType,
            MonthlyBudget = request.MonthlyBudget,
            IsActive = request.IsActive
        };

        _db.AccountingCategories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, MapCategory(category));
    }

    [HttpGet("entries")]
    public async Task<ActionResult<IEnumerable<AccountingEntryDto>>> GetEntries(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? type,
        [FromQuery] Guid? categoryId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        var query = _db.AccountingEntries
            .Include(x => x.Category)
            .AsNoTracking();

        var fromUtc = ToUtcStartOfDay(from);
        var toUtc = ToUtcEndOfDay(to);

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.Date >= fromUtc.Value);
        }
        if (toUtc.HasValue)
        {
            query = query.Where(x => x.Date <= toUtc.Value);
        }
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<AccountingEntryType>(type, true, out var parsed))
        {
            query = query.Where(x => x.Type == parsed);
        }
        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var total = await query.CountAsync();

        if (page.HasValue || pageSize.HasValue)
        {
            var pageValue = Math.Max(1, page ?? 1);
            var sizeValue = Math.Clamp(pageSize ?? 100, 1, 200);
            query = query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.CreatedAt)
                .Skip((pageValue - 1) * sizeValue)
                .Take(sizeValue);

            Response.Headers["X-Total-Count"] = total.ToString();
            Response.Headers["X-Page"] = pageValue.ToString();
            Response.Headers["X-Page-Size"] = sizeValue.ToString();
        }
        else
        {
            query = query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.CreatedAt)
                .Take(500);
        }

        var entries = await query.ToListAsync();

        return Ok(entries.Select(MapEntry));
    }

    [HttpPost("entries")]
    public async Task<ActionResult<AccountingEntryDto>> CreateEntry(AccountingEntryRequest request)
    {
        var category = await _db.AccountingCategories.FindAsync(request.CategoryId);
        if (category == null)
        {
            return BadRequest("Categoría inválida.");
        }

        if (!Enum.TryParse<AccountingEntryType>(request.Type, true, out var entryType))
        {
            return BadRequest("Tipo de movimiento inválido.");
        }

        PaymentMethod? paymentMethod = null;
        if (!string.IsNullOrWhiteSpace(request.PaymentMethod) &&
            Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var method))
        {
            paymentMethod = method;
        }

        var entryDate = NormalizeToUtcDate(request.Date);
        var entry = new AccountingEntry
        {
            Id = Guid.NewGuid(),
            Date = entryDate,
            Type = entryType,
            CategoryId = category.Id,
            Description = request.Description,
            Amount = request.Amount,
            PaymentMethod = paymentMethod,
            Reference = request.Reference,
            Source = "Manual",
            CreatedAt = DateTime.UtcNow
        };

        _db.AccountingEntries.Add(entry);
        await _db.SaveChangesAsync();

        entry.Category = category;
        return Ok(MapEntry(entry));
    }

    [HttpPut("entries/{id}")]
    public async Task<ActionResult<AccountingEntryDto>> UpdateEntry(Guid id, AccountingEntryRequest request)
    {
        var entry = await _db.AccountingEntries
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entry == null)
        {
            return NotFound("Movimiento no encontrado.");
        }

        if (entry.Source == "Invoice")
        {
            return BadRequest("No se pueden editar movimientos generados automáticamente desde facturas.");
        }

        var category = await _db.AccountingCategories.FindAsync(request.CategoryId);
        if (category == null)
        {
            return BadRequest("Categoría inválida.");
        }

        if (!Enum.TryParse<AccountingEntryType>(request.Type, true, out var entryType))
        {
            return BadRequest("Tipo de movimiento inválido.");
        }

        PaymentMethod? paymentMethod = null;
        if (!string.IsNullOrWhiteSpace(request.PaymentMethod) &&
            Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var method))
        {
            paymentMethod = method;
        }

        entry.Date = NormalizeToUtcDate(request.Date);
        entry.Type = entryType;
        entry.CategoryId = category.Id;
        entry.Description = request.Description;
        entry.Amount = request.Amount;
        entry.PaymentMethod = paymentMethod;
        entry.Reference = request.Reference;

        await _db.SaveChangesAsync();

        entry.Category = category;
        return Ok(MapEntry(entry));
    }

    [HttpDelete("entries/{id}")]
    public async Task<IActionResult> DeleteEntry(Guid id)
    {
        var entry = await _db.AccountingEntries.FindAsync(id);
        if (entry == null)
        {
            return NotFound("Movimiento no encontrado.");
        }

        if (entry.Source == "Invoice")
        {
            return BadRequest("No se pueden eliminar movimientos generados automáticamente desde facturas.");
        }

        _db.AccountingEntries.Remove(entry);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AccountingSummaryDto>> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var rangeStart = ToUtcStartOfDay(from) ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = ToUtcEndOfDay(to) ?? rangeStart.AddMonths(1).AddTicks(-1);

        var entries = await _db.AccountingEntries
            .Include(x => x.Category)
            .AsNoTracking()
            .Where(x => x.Date >= rangeStart && x.Date <= rangeEnd)
            .ToListAsync();

        var totalIncome = entries.Where(x => x.Type == AccountingEntryType.Income).Sum(x => x.Amount);
        var totalExpense = entries.Where(x => x.Type == AccountingEntryType.Expense).Sum(x => x.Amount);

        var groups = entries
            .GroupBy(x => new { x.Category.Group, x.Type })
            .Select(g => new AccountingGroupSummaryDto
            {
                Group = g.Key.Group,
                Type = g.Key.Type.ToString(),
                Total = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        return Ok(new AccountingSummaryDto
        {
            From = rangeStart,
            To = rangeEnd,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Net = totalIncome - totalExpense,
            Groups = groups
        });
    }

    private static AccountingCategoryDto MapCategory(AccountingCategory category)
    {
        return new AccountingCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Group = category.Group,
            Type = category.Type.ToString(),
            MonthlyBudget = category.MonthlyBudget,
            IsActive = category.IsActive
        };
    }

    private static AccountingEntryDto MapEntry(AccountingEntry entry)
    {
        return new AccountingEntryDto
        {
            Id = entry.Id,
            Date = entry.Date,
            Type = entry.Type.ToString(),
            CategoryId = entry.CategoryId,
            CategoryName = entry.Category?.Name ?? string.Empty,
            CategoryGroup = entry.Category?.Group ?? string.Empty,
            Description = entry.Description,
            Amount = entry.Amount,
            PaymentMethod = entry.PaymentMethod?.ToString(),
            Reference = entry.Reference,
            Source = entry.Source,
            InvoiceId = entry.InvoiceId
        };
    }

    private static DateTime NormalizeToUtcDate(DateTime date)
    {
        var normalized = date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };

        // Mantener solo la fecha (00:00 UTC) para movimientos contables
        var utcDate = normalized.Date;
        return DateTime.SpecifyKind(utcDate, DateTimeKind.Utc);
    }

    private static DateTime? ToUtcStartOfDay(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        var normalized = value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };

        return DateTime.SpecifyKind(normalized.Date, DateTimeKind.Utc);
    }

    private static DateTime? ToUtcEndOfDay(DateTime? value)
    {
        var start = ToUtcStartOfDay(value);
        return start?.AddDays(1).AddTicks(-1);
    }
}
