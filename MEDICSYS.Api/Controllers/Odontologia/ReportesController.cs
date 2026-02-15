using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using System.Security.Claims;

namespace MEDICSYS.Api.Controllers.Odontologia;

[Authorize(Roles = "Odontologo")]
[ApiController]
[Route("api/odontologia/reportes")]
public class ReportesController : ControllerBase
{
    private readonly OdontologoDbContext _db;
    private readonly ILogger<ReportesController> _logger;

    public ReportesController(OdontologoDbContext db, ILogger<ReportesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Guid GetOdontologoId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("financiero")]
    public async Task<ActionResult<object>> GetFinancialReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var odontologoId = GetOdontologoId();
        var start = ToUtcStartOfDay(startDate) ?? DateTime.UtcNow.AddMonths(-6);
        var end = ToUtcEndOfDay(endDate) ?? DateTime.UtcNow;

        // Calcular ingresos totales basado en gastos negativos (simulado)
        // En una implementación real, aquí se obtendrían las ventas reales
        var incomeByMonth = new List<object>();

        // Obtener gastos por mes
        var expenses = await _db.Expenses
            .Where(e => e.OdontologoId == odontologoId && 
                       e.ExpenseDate >= start && 
                       e.ExpenseDate <= end)
            .ToListAsync();

        var expensesByMonth = expenses
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Amount = g.Sum(e => e.Amount)
            })
            .OrderBy(x => x.Month)
            .ToList();

        // Gastos por categoría
        var expensesByCategory = expenses
            .GroupBy(e => e.Category)
            .Select(g => new
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        // Compras por mes
        var purchases = await _db.PurchaseOrders
            .Where(p => p.OdontologoId == odontologoId && 
                       p.PurchaseDate >= start && 
                       p.PurchaseDate <= end)
            .ToListAsync();

        var purchasesByMonth = purchases
            .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month })
            .Select(g => new
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Amount = g.Sum(p => p.Total)
            })
            .OrderBy(x => x.Month)
            .ToList();

        // Estado del inventario
        var inventoryStatus = await _db.InventoryItems
            .Where(i => i.OdontologoId == odontologoId)
            .Select(i => new
            {
                TotalItems = 1,
                i.Quantity,
                i.MinimumQuantity,
                Value = i.Quantity * i.UnitPrice,
                IsLowStock = i.Quantity <= i.MinimumQuantity
            })
            .ToListAsync();

        var inventorySummary = new
        {
            TotalItems = inventoryStatus.Count,
            TotalValue = inventoryStatus.Sum(i => i.Value),
            LowStockItems = inventoryStatus.Count(i => i.IsLowStock),
            AverageStock = inventoryStatus.Any() ? inventoryStatus.Average(i => i.Quantity) : 0
        };

        // Totales
        var totalIncome = 0m; // TODO: Calcular con tabla de ventas real
        var totalExpenses = expensesByMonth.Sum(e => e.Amount);
        var totalPurchases = purchasesByMonth.Sum(p => p.Amount);
        var profit = totalIncome - totalExpenses;

        return Ok(new
        {
            Period = new { Start = start, End = end },
            Summary = new
            {
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                TotalPurchases = totalPurchases,
                Profit = profit,
                ProfitMargin = totalIncome > 0 ? (profit / totalIncome) * 100 : 0
            },
            IncomeByMonth = incomeByMonth,
            ExpensesByMonth = expensesByMonth,
            PurchasesByMonth = purchasesByMonth,
            ExpensesByCategory = expensesByCategory,
            InventorySummary = inventorySummary
        });
    }

    [HttpGet("ventas")]
    public async Task<ActionResult<object>> GetSalesReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var odontologoId = GetOdontologoId();
        var start = ToUtcStartOfDay(startDate) ?? DateTime.UtcNow.AddMonths(-1);
        var end = ToUtcEndOfDay(endDate) ?? DateTime.UtcNow;

        // TODO: Implementar con tabla de ventas correcta
        // Por ahora retornamos datos simulados
        return Ok(new
        {
            Period = new { Start = start, End = end },
            Summary = new
            {
                TotalSales = 0m,
                InvoiceCount = 0,
                AverageTicket = 0m
            },
            SalesByDay = new List<object>(),
            TopServices = new List<object>(),
            PaymentMethods = new List<object>()
        });
    }

    [HttpGet("comparativo")]
    public async Task<ActionResult<object>> GetComparativeReport(
        [FromQuery] int months = 12)
    {
        var odontologoId = GetOdontologoId();
        var startDate = DateTime.UtcNow.AddMonths(-months);

        // TODO: Usar tabla de ingresos correcta
        var expenses = await _db.Expenses
            .Where(e => e.OdontologoId == odontologoId && e.ExpenseDate >= startDate)
            .ToListAsync();

        var monthlyData = Enumerable.Range(0, months)
            .Select(i =>
            {
                var month = DateTime.UtcNow.AddMonths(-i);
                var monthKey = $"{month.Year}-{month.Month:D2}";

                var monthIncome = 0m; // TODO: Calcular ingresos reales

                var monthExpenses = expenses
                    .Where(exp => exp.ExpenseDate.Year == month.Year && exp.ExpenseDate.Month == month.Month)
                    .Sum(exp => exp.Amount);

                return new
                {
                    Month = monthKey,
                    MonthName = month.ToString("MMMM yyyy"),
                    Income = monthIncome,
                    Expenses = monthExpenses,
                    Profit = monthIncome - monthExpenses
                };
            })
            .OrderBy(x => x.Month)
            .ToList();

        return Ok(new
        {
            Months = months,
            Data = monthlyData,
            AverageIncome = monthlyData.Average(m => m.Income),
            AverageExpenses = monthlyData.Average(m => m.Expenses),
            AverageProfit = monthlyData.Average(m => m.Profit)
        });
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
