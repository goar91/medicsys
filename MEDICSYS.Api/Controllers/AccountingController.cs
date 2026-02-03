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
    private readonly AppDbContext _db;

    public AccountingController(AppDbContext db)
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

    [HttpGet("entries")]
    public async Task<ActionResult<IEnumerable<AccountingEntryDto>>> GetEntries([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? type, [FromQuery] Guid? categoryId)
    {
        var query = _db.AccountingEntries
            .Include(x => x.Category)
            .AsNoTracking();

        if (from.HasValue)
        {
            query = query.Where(x => x.Date >= from.Value.Date);
        }
        if (to.HasValue)
        {
            var end = to.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.Date <= end);
        }
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<AccountingEntryType>(type, true, out var parsed))
        {
            query = query.Where(x => x.Type == parsed);
        }
        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var entries = await query
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .Take(500)
            .ToListAsync();

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

        var entry = new AccountingEntry
        {
            Id = Guid.NewGuid(),
            Date = request.Date.Date,
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

    [HttpGet("summary")]
    public async Task<ActionResult<AccountingSummaryDto>> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var rangeStart = from?.Date ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var rangeEnd = (to?.Date ?? rangeStart.AddMonths(1)).AddDays(1).AddTicks(-1);

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
}
