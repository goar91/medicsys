using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Services;
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

    private IQueryable<Invoice> GetOwnedInvoices(Guid odontologoId)
    {
        return _db.Invoices.Where(i => _db.OdontologoInvoiceOwnerships
            .Any(o => o.InvoiceId == i.Id && o.OdontologoId == odontologoId));
    }

    [HttpGet("financiero")]
    public async Task<ActionResult<object>> GetFinancialReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var odontologoId = GetOdontologoId();
        var start = ToUtcStartOfDay(startDate) ?? DateTimeHelper.Now().AddMonths(-6);
        var end = ToUtcEndOfDay(endDate) ?? DateTimeHelper.Now();
        var monthKeys = BuildMonthKeys(start, end);

        var incomeMonthlyRaw = await GetOwnedInvoices(odontologoId)
            .AsNoTracking()
            .Where(i =>
                i.IssuedAt >= start &&
                i.IssuedAt <= end &&
                i.Status != InvoiceStatus.Rejected)
            .GroupBy(i => new { i.IssuedAt.Year, i.IssuedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Amount = g.Sum(i => i.TotalToCharge)
            })
            .ToListAsync();

        var incomeMonthlyData = incomeMonthlyRaw.ToDictionary(
            x => FormatMonthKey(x.Year, x.Month),
            x => x.Amount);

        var incomeByMonth = monthKeys
            .Select(monthKey => new
            {
                Month = monthKey,
                Amount = incomeMonthlyData.TryGetValue(monthKey, out var amount) ? amount : 0m
            })
            .ToList();

        var expensesMonthlyRaw = await _db.Expenses
            .AsNoTracking()
            .Where(e =>
                e.OdontologoId == odontologoId &&
                e.ExpenseDate >= start &&
                e.ExpenseDate <= end)
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Amount = g.Sum(e => e.Amount)
            })
            .ToListAsync();

        var expensesMonthlyData = expensesMonthlyRaw.ToDictionary(
            x => FormatMonthKey(x.Year, x.Month),
            x => x.Amount);

        var expensesByMonth = monthKeys
            .Select(monthKey => new
            {
                Month = monthKey,
                Amount = expensesMonthlyData.TryGetValue(monthKey, out var amount) ? amount : 0m
            })
            .ToList();

        var expensesByCategory = await _db.Expenses
            .AsNoTracking()
            .Where(e =>
                e.OdontologoId == odontologoId &&
                e.ExpenseDate >= start &&
                e.ExpenseDate <= end)
            .GroupBy(e => e.Category)
            .Select(g => new
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToListAsync();

        var purchasesMonthlyRaw = await _db.PurchaseOrders
            .AsNoTracking()
            .Where(p =>
                p.OdontologoId == odontologoId &&
                p.PurchaseDate >= start &&
                p.PurchaseDate <= end)
            .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Amount = g.Sum(p => p.Total)
            })
            .ToListAsync();

        var purchasesMonthlyData = purchasesMonthlyRaw.ToDictionary(
            x => FormatMonthKey(x.Year, x.Month),
            x => x.Amount);

        var purchasesByMonth = monthKeys
            .Select(monthKey => new
            {
                Month = monthKey,
                Amount = purchasesMonthlyData.TryGetValue(monthKey, out var amount) ? amount : 0m
            })
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
        var totalIncome = incomeByMonth.Sum(i => i.Amount);
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
        var start = ToUtcStartOfDay(startDate) ?? DateTimeHelper.Now().AddMonths(-1);
        var end = ToUtcEndOfDay(endDate) ?? DateTimeHelper.Now();
        var invoices = await GetOwnedInvoices(odontologoId)
            .AsNoTracking()
            .Where(i =>
                i.IssuedAt >= start &&
                i.IssuedAt <= end &&
                i.Status != InvoiceStatus.Rejected)
            .ToListAsync();

        var totalSales = invoices.Sum(i => i.TotalToCharge);
        var invoiceCount = invoices.Count;

        var salesByDay = invoices
            .GroupBy(i => DateTime.SpecifyKind(i.IssuedAt.Date, DateTimeKind.Utc))
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Count = g.Count(),
                Amount = g.Sum(i => i.TotalToCharge)
            })
            .ToList();

        var ownedInvoiceIds = _db.OdontologoInvoiceOwnerships
            .Where(x => x.OdontologoId == odontologoId)
            .Select(x => x.InvoiceId);

        var topServices = await _db.InvoiceItems
            .AsNoTracking()
            .Where(item =>
                ownedInvoiceIds.Contains(item.InvoiceId) &&
                item.Invoice.IssuedAt >= start &&
                item.Invoice.IssuedAt <= end &&
                item.Invoice.Status != InvoiceStatus.Rejected)
            .GroupBy(item => item.Description)
            .Select(g => new
            {
                Service = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(10)
            .ToListAsync();

        var paymentMethods = invoices
            .GroupBy(i => i.PaymentMethod)
            .Select(g => new
            {
                Method = g.Key.ToString(),
                Count = g.Count(),
                Amount = g.Sum(i => i.TotalToCharge)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return Ok(new
        {
            Period = new { Start = start, End = end },
            Summary = new
            {
                TotalSales = totalSales,
                InvoiceCount = invoiceCount,
                AverageTicket = invoiceCount > 0 ? totalSales / invoiceCount : 0m
            },
            SalesByDay = salesByDay,
            TopServices = topServices,
            PaymentMethods = paymentMethods
        });
    }

    [HttpGet("comparativo")]
    public async Task<ActionResult<object>> GetComparativeReport(
        [FromQuery] int months = 12)
    {
        var odontologoId = GetOdontologoId();
        var normalizedMonths = Math.Clamp(months, 1, 36);
        var now = DateTimeHelper.Now();
        var startDate = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc)
            .AddMonths(-(normalizedMonths - 1));

        var incomeMonthlyRaw = await GetOwnedInvoices(odontologoId)
            .AsNoTracking()
            .Where(i =>
                i.IssuedAt >= startDate &&
                i.IssuedAt <= now &&
                i.Status != InvoiceStatus.Rejected)
            .GroupBy(i => new { i.IssuedAt.Year, i.IssuedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Amount = g.Sum(i => i.TotalToCharge)
            })
            .ToListAsync();

        var incomeMonthlyData = incomeMonthlyRaw.ToDictionary(
            x => FormatMonthKey(x.Year, x.Month),
            x => x.Amount);

        var expenseMonthlyRaw = await _db.Expenses
            .AsNoTracking()
            .Where(e =>
                e.OdontologoId == odontologoId &&
                e.ExpenseDate >= startDate &&
                e.ExpenseDate <= now)
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Amount = g.Sum(e => e.Amount)
            })
            .ToListAsync();

        var expenseMonthlyData = expenseMonthlyRaw.ToDictionary(
            x => FormatMonthKey(x.Year, x.Month),
            x => x.Amount);

        var monthlyData = Enumerable.Range(0, normalizedMonths)
            .Select(offset =>
            {
                var month = startDate.AddMonths(offset);
                var monthKey = FormatMonthKey(month.Year, month.Month);
                var monthIncome = incomeMonthlyData.TryGetValue(monthKey, out var income) ? income : 0m;
                var monthExpenses = expenseMonthlyData.TryGetValue(monthKey, out var expenses) ? expenses : 0m;
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
            Months = normalizedMonths,
            Data = monthlyData,
            AverageIncome = monthlyData.Average(m => m.Income),
            AverageExpenses = monthlyData.Average(m => m.Expenses),
            AverageProfit = monthlyData.Average(m => m.Profit)
        });
    }

    [HttpGet("avanzado")]
    public async Task<ActionResult<object>> GetAdvancedReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var odontologoId = GetOdontologoId();
        var start = ToUtcStartOfDay(startDate) ?? DateTimeHelper.Now().AddMonths(-6);
        var end = ToUtcEndOfDay(endDate) ?? DateTimeHelper.Now();

        var ownedInvoices = GetOwnedInvoices(odontologoId);

        var invoices = await ownedInvoices
            .AsNoTracking()
            .Where(i => i.IssuedAt >= start && i.IssuedAt <= end && i.Status != InvoiceStatus.Rejected)
            .ToListAsync();

        var invoiceIds = invoices.Select(i => i.Id).ToList();

        var procedureRevenue = await _db.InvoiceItems
            .AsNoTracking()
            .Where(i => invoiceIds.Contains(i.InvoiceId))
            .GroupBy(i => i.Description)
            .Select(g => new
            {
                Procedure = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync();

        var expenses = await _db.Expenses
            .AsNoTracking()
            .Where(e => e.OdontologoId == odontologoId && e.ExpenseDate >= start && e.ExpenseDate <= end)
            .SumAsync(e => e.Amount);

        var purchases = await _db.PurchaseOrders
            .AsNoTracking()
            .Where(p => p.OdontologoId == odontologoId && p.PurchaseDate >= start && p.PurchaseDate <= end)
            .SumAsync(p => p.Total);

        var totalRevenue = procedureRevenue.Sum(x => x.Revenue);
        var operationalCost = expenses + purchases;
        var estimatedCostRatio = totalRevenue > 0 ? operationalCost / totalRevenue : 0m;

        var profitabilityByProcedure = procedureRevenue.Select(x =>
        {
            var estimatedCost = Math.Round(x.Revenue * estimatedCostRatio, 2);
            var estimatedProfit = x.Revenue - estimatedCost;
            return new
            {
                x.Procedure,
                x.Quantity,
                x.Revenue,
                EstimatedCost = estimatedCost,
                EstimatedProfit = estimatedProfit,
                MarginPercent = x.Revenue > 0 ? Math.Round((estimatedProfit / x.Revenue) * 100m, 2) : 0m
            };
        }).ToList();

        var marketingExpenses = await _db.Expenses
            .AsNoTracking()
            .Where(e =>
                e.OdontologoId == odontologoId &&
                e.ExpenseDate >= start &&
                e.ExpenseDate <= end &&
                (EF.Functions.ILike(e.Category, "%marketing%") ||
                 EF.Functions.ILike(e.Category, "%publicidad%") ||
                 EF.Functions.ILike(e.Category, "%anuncio%") ||
                 EF.Functions.ILike(e.Category, "%mercadeo%")))
            .SumAsync(e => e.Amount);

        var newPatients = await _db.OdontologoPatients
            .AsNoTracking()
            .Where(p => p.OdontologoId == odontologoId && p.CreatedAt >= start && p.CreatedAt <= end)
            .CountAsync();

        var customerLtv = invoices
            .GroupBy(i => i.CustomerIdentification)
            .Select(g => new
            {
                CustomerIdentification = g.Key,
                CustomerName = g.Last().CustomerName,
                Revenue = g.Sum(x => x.TotalToCharge),
                InvoiceCount = g.Count(),
                AverageTicket = g.Count() > 0 ? g.Average(x => x.TotalToCharge) : 0m
            })
            .OrderByDescending(x => x.Revenue)
            .Take(20)
            .ToList();

        var ltvValue = customerLtv.Any() ? customerLtv.Average(x => x.Revenue) : 0m;

        return Ok(new
        {
            Period = new { Start = start, End = end },
            Summary = new
            {
                TotalRevenue = totalRevenue,
                OperationalCost = operationalCost,
                EstimatedGrossProfit = totalRevenue - operationalCost,
                EstimatedGrossMargin = totalRevenue > 0 ? Math.Round(((totalRevenue - operationalCost) / totalRevenue) * 100m, 2) : 0m,
                NewPatients = newPatients,
                MarketingExpense = marketingExpenses,
                PatientAcquisitionCost = newPatients > 0 ? Math.Round(marketingExpenses / newPatients, 2) : 0m,
                Ltv = Math.Round(ltvValue, 2)
            },
            ProfitabilityByProcedure = profitabilityByProcedure,
            CustomerLifetimeValue = customerLtv
        });
    }

    private static List<string> BuildMonthKeys(DateTime start, DateTime end)
    {
        var keys = new List<string>();
        var cursor = DateTime.SpecifyKind(new DateTime(start.Year, start.Month, 1), DateTimeKind.Utc);
        var limit = DateTime.SpecifyKind(new DateTime(end.Year, end.Month, 1), DateTimeKind.Utc);

        while (cursor <= limit)
        {
            keys.Add(FormatMonthKey(cursor.Year, cursor.Month));
            cursor = cursor.AddMonths(1);
        }

        return keys;
    }

    private static string FormatMonthKey(int year, int month) => $"{year}-{month:D2}";

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
