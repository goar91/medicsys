using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Contracts;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Models.Odontologia;
using System.Security.Claims;
using MEDICSYS.Api.Services;

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
        var now = DateTimeHelper.Now();
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
            CreatedAt = DateTimeHelper.Now()
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        await SyncAccountingEntryAsync(expense);

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
        expense.UpdatedAt = DateTimeHelper.Now();

        await _db.SaveChangesAsync();

        await SyncAccountingEntryAsync(expense);

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

        // Eliminar el asiento contable asociado
        var accountingEntry = await _db.AccountingEntries
            .FirstOrDefaultAsync(e => e.Source == "Expense" && e.Reference == expense.Id.ToString());
        if (accountingEntry != null)
        {
            _db.AccountingEntries.Remove(accountingEntry);
        }

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private async Task SyncAccountingEntryAsync(Expense expense)
    {
        // Mapear categoría del gasto a la categoría contable más adecuada
        var categoryName = MapExpenseCategoryToAccountingCategory(expense.Category);
        var category = await _db.AccountingCategories
            .FirstOrDefaultAsync(c => c.Name == categoryName && c.Type == AccountingEntryType.Expense);

        if (category == null)
        {
            category = new AccountingCategory
            {
                Id = Guid.NewGuid(),
                Name = categoryName,
                Group = "Gastos Operativos",
                Type = AccountingEntryType.Expense,
                MonthlyBudget = 0,
                IsActive = true
            };
            _db.AccountingCategories.Add(category);
            await _db.SaveChangesAsync();
        }

        // Mapear el método de pago
        PaymentMethod? paymentMethod = expense.PaymentMethod switch
        {
            "Efectivo" => Models.PaymentMethod.Cash,
            "Tarjeta" => Models.PaymentMethod.Card,
            "Transferencia" => Models.PaymentMethod.Transfer,
            _ => null
        };

        // Buscar si ya existe un asiento contable para este gasto
        var existingEntry = await _db.AccountingEntries
            .FirstOrDefaultAsync(e => e.Source == "Expense" && e.Reference == expense.Id.ToString());

        if (existingEntry != null)
        {
            // Actualizar el asiento existente
            existingEntry.Date = expense.ExpenseDate;
            existingEntry.CategoryId = category.Id;
            existingEntry.Description = expense.Description;
            existingEntry.Amount = expense.Amount;
            existingEntry.PaymentMethod = paymentMethod;
        }
        else
        {
            // Crear nuevo asiento contable
            var entry = new AccountingEntry
            {
                Id = Guid.NewGuid(),
                Date = expense.ExpenseDate,
                Type = AccountingEntryType.Expense,
                CategoryId = category.Id,
                Description = expense.Description,
                Amount = expense.Amount,
                PaymentMethod = paymentMethod,
                Reference = expense.Id.ToString(),
                Source = "Expense",
                CreatedAt = DateTimeHelper.Now()
            };
            _db.AccountingEntries.Add(entry);
        }

        await _db.SaveChangesAsync();
    }

    private static string MapExpenseCategoryToAccountingCategory(string category)
    {
        return category switch
        {
            "Supplies" => "Suministros varios",
            "Equipment" => "Compra de Materias Primas o Mercadería",
            "Maintenance" => "Suministros varios",
            "Utilities" => "Suministros varios",
            "Rent" => "Cánones /Arrendamiento",
            "Salaries" => "Sueldos y Salarios",
            "Marketing" => "Publicidad",
            "Professional" => "Honorarios Profesionales",
            _ => "Otros"
        };
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
