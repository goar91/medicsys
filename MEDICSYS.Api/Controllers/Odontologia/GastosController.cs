using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Contracts;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models.Odontologia;
using System.Security.Claims;

namespace MEDICSYS.Api.Controllers.Odontologia;

[Authorize(Roles = "Odontologo")]
[ApiController]
[Route("api/odontologia/gastos")]
public class GastosController : ControllerBase
{
    private readonly OdontologoDbContext _db;
    private readonly ILogger<GastosController> _logger;

    public GastosController(OdontologoDbContext db, ILogger<GastosController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Guid GetOdontologoId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<List<ExpenseDto>>> GetExpenses(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? category,
        [FromQuery] string? paymentMethod,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        var odontologoId = GetOdontologoId();
        var startUtc = ToUtcStartOfDay(startDate);
        var endUtc = ToUtcEndOfDay(endDate);

        var query = _db.Expenses
            .Where(e => e.OdontologoId == odontologoId)
            .AsNoTracking();

        if (startUtc.HasValue)
            query = query.Where(e => e.ExpenseDate >= startUtc.Value);

        if (endUtc.HasValue)
            query = query.Where(e => e.ExpenseDate <= endUtc.Value);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(e => e.Category == category);

        if (!string.IsNullOrEmpty(paymentMethod))
            query = query.Where(e => e.PaymentMethod == paymentMethod);

        var total = await query.CountAsync();

        if (page.HasValue || pageSize.HasValue)
        {
            var pageValue = Math.Max(1, page ?? 1);
            var sizeValue = Math.Clamp(pageSize ?? 50, 1, 200);
            query = query
                .OrderByDescending(e => e.ExpenseDate)
                .Skip((pageValue - 1) * sizeValue)
                .Take(sizeValue);

            Response.Headers["X-Total-Count"] = total.ToString();
            Response.Headers["X-Page"] = pageValue.ToString();
            Response.Headers["X-Page-Size"] = sizeValue.ToString();
        }
        else
        {
            query = query.OrderByDescending(e => e.ExpenseDate);
        }

        var expenses = await query
            .Select(e => new ExpenseDto(
                e.Id,
                e.OdontologoId,
                e.Description,
                e.Amount,
                e.ExpenseDate,
                e.Category,
                e.PaymentMethod,
                e.InvoiceNumber,
                e.Supplier,
                e.Notes,
                e.CreatedAt,
                e.UpdatedAt
            ))
            .ToListAsync();

        return Ok(expenses);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ExpenseSummaryDto>> GetSummary()
    {
        var odontologoId = GetOdontologoId();
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekStart = now.AddDays(-7);

        var allExpenses = await _db.Expenses
            .Where(e => e.OdontologoId == odontologoId)
            .ToListAsync();

        var totalExpenses = allExpenses.Sum(e => e.Amount);
        var monthExpenses = allExpenses
            .Where(e => e.ExpenseDate >= monthStart)
            .Sum(e => e.Amount);
        var weekExpenses = allExpenses
            .Where(e => e.ExpenseDate >= weekStart)
            .Sum(e => e.Amount);

        var expensesByCategory = allExpenses
            .Where(e => e.ExpenseDate >= monthStart)
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var recentExpenses = allExpenses
            .OrderByDescending(e => e.ExpenseDate)
            .Take(10)
            .Select(e => new ExpenseDto(
                e.Id,
                e.OdontologoId,
                e.Description,
                e.Amount,
                e.ExpenseDate,
                e.Category,
                e.PaymentMethod,
                e.InvoiceNumber,
                e.Supplier,
                e.Notes,
                e.CreatedAt,
                e.UpdatedAt
            ))
            .ToList();

        var summary = new ExpenseSummaryDto(
            totalExpenses,
            monthExpenses,
            weekExpenses,
            expensesByCategory,
            recentExpenses
        );

        return Ok(summary);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExpenseDto>> GetExpense(Guid id)
    {
        var odontologoId = GetOdontologoId();

        var expense = await _db.Expenses
            .Where(e => e.Id == id && e.OdontologoId == odontologoId)
            .Select(e => new ExpenseDto(
                e.Id,
                e.OdontologoId,
                e.Description,
                e.Amount,
                e.ExpenseDate,
                e.Category,
                e.PaymentMethod,
                e.InvoiceNumber,
                e.Supplier,
                e.Notes,
                e.CreatedAt,
                e.UpdatedAt
            ))
            .FirstOrDefaultAsync();

        if (expense == null)
            return NotFound();

        return Ok(expense);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> CreateExpense(ExpenseCreateRequest request)
    {
        var odontologoId = GetOdontologoId();

        var expense = new Expense
        {
            OdontologoId = odontologoId,
            Description = request.Description,
            Amount = request.Amount,
            ExpenseDate = DateTime.SpecifyKind(request.ExpenseDate, DateTimeKind.Utc),
            Category = request.Category,
            PaymentMethod = request.PaymentMethod,
            InvoiceNumber = request.InvoiceNumber,
            Supplier = request.Supplier,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        var dto = new ExpenseDto(
            expense.Id,
            expense.OdontologoId,
            expense.Description,
            expense.Amount,
            expense.ExpenseDate,
            expense.Category,
            expense.PaymentMethod,
            expense.InvoiceNumber,
            expense.Supplier,
            expense.Notes,
            expense.CreatedAt,
            expense.UpdatedAt
        );

        return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExpenseDto>> UpdateExpense(Guid id, ExpenseUpdateRequest request)
    {
        var odontologoId = GetOdontologoId();

        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == id && e.OdontologoId == odontologoId);

        if (expense == null)
            return NotFound();

        expense.Description = request.Description;
        expense.Amount = request.Amount;
        expense.ExpenseDate = DateTime.SpecifyKind(request.ExpenseDate, DateTimeKind.Utc);
        expense.Category = request.Category;
        expense.PaymentMethod = request.PaymentMethod;
        expense.InvoiceNumber = request.InvoiceNumber;
        expense.Supplier = request.Supplier;
        expense.Notes = request.Notes;
        expense.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var dto = new ExpenseDto(
            expense.Id,
            expense.OdontologoId,
            expense.Description,
            expense.Amount,
            expense.ExpenseDate,
            expense.Category,
            expense.PaymentMethod,
            expense.InvoiceNumber,
            expense.Supplier,
            expense.Notes,
            expense.CreatedAt,
            expense.UpdatedAt
        );

        return Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(Guid id)
    {
        var odontologoId = GetOdontologoId();

        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == id && e.OdontologoId == odontologoId);

        if (expense == null)
            return NotFound();

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();

        return NoContent();
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
