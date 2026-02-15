using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Contracts;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = Roles.Odontologo)]
public class InvoicesController : ControllerBase
{
    private const decimal VatRate = 0.15m;
    private const string SriEnvironmentPruebas = "Pruebas";
    private const string SriEnvironmentProduccion = "Produccion";

    private readonly OdontologoDbContext _db;
    private readonly ISriService _sri;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(OdontologoDbContext db, ISriService sri, ILogger<InvoicesController> logger)
    {
        _db = db;
        _sri = sri;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        var query = _db.Invoices
            .Include(x => x.Items)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvoiceStatus>(status, true, out var parsed))
        {
            query = query.Where(x => x.Status == parsed);
        }

        var total = await query.CountAsync();

        if (page.HasValue || pageSize.HasValue)
        {
            var pageValue = Math.Max(1, page ?? 1);
            var sizeValue = Math.Clamp(pageSize ?? 50, 1, 200);
            query = query
                .OrderByDescending(x => x.IssuedAt)
                .Skip((pageValue - 1) * sizeValue)
                .Take(sizeValue);

            Response.Headers["X-Total-Count"] = total.ToString();
            Response.Headers["X-Page"] = pageValue.ToString();
            Response.Headers["X-Page-Size"] = sizeValue.ToString();
        }
        else
        {
            query = query.OrderByDescending(x => x.IssuedAt);
        }

        var invoices = await query.ToListAsync();
        return Ok(invoices.Select(Map));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id)
    {
        var invoice = await _db.Invoices
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (invoice == null)
        {
            return NotFound();
        }

        return Ok(Map(invoice));
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(InvoiceCreateRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest("Debe incluir al menos un item.");
        }

        var method = ParsePaymentMethod(request.PaymentMethod);

        var nextSequential = (await _db.Invoices.MaxAsync(x => (int?)x.Sequential) ?? 0) + 1;
        var issuedAt = DateTime.UtcNow;
        var number = $"001-001-{nextSequential.ToString().PadLeft(9, '0')}";

        var items = request.Items.Select(item =>
        {
            var discountAmount = item.Quantity * item.UnitPrice * (item.DiscountPercent / 100m);
            var subtotal = (item.Quantity * item.UnitPrice) - discountAmount;
            var tax = subtotal * VatRate;
            var total = subtotal + tax;

            return new InvoiceItem
            {
                Id = Guid.NewGuid(),
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent,
                Subtotal = subtotal,
                TaxRate = VatRate,
                Tax = tax,
                Total = total
            };
        }).ToList();

        var subtotalSum = items.Sum(x => x.Subtotal);
        var taxSum = items.Sum(x => x.Tax);
        var total = subtotalSum + taxSum;
        var discountTotal = request.Items.Sum(x => x.Quantity * x.UnitPrice * (x.DiscountPercent / 100m));

        var cardFeePercent = method == PaymentMethod.Card ? request.CardFeePercent : null;
        var cardFeeAmount = method == PaymentMethod.Card && cardFeePercent.HasValue
            ? Math.Round(total * (cardFeePercent.Value / 100m), 2)
            : 0m;

        var totalToCharge = total + cardFeeAmount;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Number = number,
            Sequential = nextSequential,
            IssuedAt = issuedAt,
            CustomerIdentificationType = request.CustomerIdentificationType,
            CustomerIdentification = request.CustomerIdentification,
            CustomerName = request.CustomerName,
            CustomerAddress = request.CustomerAddress,
            CustomerPhone = request.CustomerPhone,
            CustomerEmail = request.CustomerEmail,
            Observations = request.Observations,
            Subtotal = subtotalSum,
            DiscountTotal = discountTotal,
            Tax = taxSum,
            Total = total,
            CardFeePercent = cardFeePercent,
            CardFeeAmount = cardFeeAmount,
            TotalToCharge = totalToCharge,
            PaymentMethod = method,
            CardType = request.CardType,
            CardInstallments = request.CardInstallments,
            PaymentReference = request.PaymentReference,
            SriEnvironment = ParseSriEnvironment(request.SriEnvironment),
            Status = InvoiceStatus.Pending,
            CreatedAt = issuedAt,
            UpdatedAt = issuedAt,
            Items = items
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        await RegisterAccountingEntryAsync(invoice);

        if (request.SendToSri)
        {
            await SendToSriInternalAsync(invoice);
        }

        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, Map(invoice));
    }

    [HttpPost("{id:guid}/send-sri")]
    public async Task<ActionResult<InvoiceDto>> SendToSri(Guid id)
    {
        var invoice = await _db.Invoices
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (invoice == null)
        {
            return NotFound();
        }

        await SendToSriInternalAsync(invoice);
        return Ok(Map(invoice));
    }

    private async Task SendToSriInternalAsync(Invoice invoice)
    {
        var result = await _sri.SendInvoiceAsync(invoice, invoice.SriEnvironment);

        invoice.SriAccessKey = result.AccessKey;
        invoice.SriAuthorizationNumber = result.AuthorizationNumber;
        invoice.SriAuthorizedAt = result.AuthorizedAt;
        invoice.SriMessages = result.Messages;
        invoice.Status = result.Status switch
        {
            "AUTORIZADO" => InvoiceStatus.Authorized,
            "RECHAZADO" => InvoiceStatus.Rejected,
            _ => InvoiceStatus.Pending
        };
        invoice.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    private async Task RegisterAccountingEntryAsync(Invoice invoice)
    {
        var category = await _db.AccountingCategories
            .FirstOrDefaultAsync(x => x.Name == "Ingresos por servicios" && x.Type == AccountingEntryType.Income);

        if (category == null)
        {
            category = new AccountingCategory
            {
                Id = Guid.NewGuid(),
                Name = "Ingresos por servicios",
                Group = "Ingresos",
                Type = AccountingEntryType.Income,
                MonthlyBudget = 0,
                IsActive = true
            };
            _db.AccountingCategories.Add(category);
            await _db.SaveChangesAsync();
        }

        var entry = new AccountingEntry
        {
            Id = Guid.NewGuid(),
            Date = DateTime.SpecifyKind(invoice.IssuedAt.Date, DateTimeKind.Utc),
            Type = AccountingEntryType.Income,
            CategoryId = category.Id,
            Description = $"Factura {invoice.Number}",
            Amount = invoice.TotalToCharge,
            PaymentMethod = invoice.PaymentMethod,
            Reference = invoice.PaymentReference,
            InvoiceId = invoice.Id,
            Source = "Invoice",
            CreatedAt = DateTime.UtcNow
        };

        _db.AccountingEntries.Add(entry);
        await _db.SaveChangesAsync();
    }

    private static PaymentMethod ParsePaymentMethod(string method)
    {
        return Enum.TryParse<PaymentMethod>(method, true, out var parsed)
            ? parsed
            : PaymentMethod.Cash;
    }

    private static string ParseSriEnvironment(string? environment)
    {
        if (string.Equals(environment, SriEnvironmentProduccion, StringComparison.OrdinalIgnoreCase))
        {
            return SriEnvironmentProduccion;
        }

        return SriEnvironmentPruebas;
    }

    private static InvoiceDto Map(Invoice invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            Number = invoice.Number,
            Sequential = invoice.Sequential,
            IssuedAt = invoice.IssuedAt,
            Customer = new InvoiceCustomerDto
            {
                IdentificationType = invoice.CustomerIdentificationType,
                Identification = invoice.CustomerIdentification,
                Name = invoice.CustomerName,
                Address = invoice.CustomerAddress,
                Phone = invoice.CustomerPhone,
                Email = invoice.CustomerEmail
            },
            Subtotal = invoice.Subtotal,
            DiscountTotal = invoice.DiscountTotal,
            Tax = invoice.Tax,
            Total = invoice.Total,
            CardFeePercent = invoice.CardFeePercent,
            CardFeeAmount = invoice.CardFeeAmount,
            TotalToCharge = invoice.TotalToCharge,
            PaymentMethod = invoice.PaymentMethod.ToString(),
            CardType = invoice.CardType,
            CardInstallments = invoice.CardInstallments,
            PaymentReference = invoice.PaymentReference,
            Observations = invoice.Observations,
            Status = invoice.Status.ToString(),
            SriAccessKey = invoice.SriAccessKey,
            SriAuthorizationNumber = invoice.SriAuthorizationNumber,
            SriAuthorizedAt = invoice.SriAuthorizedAt,
            SriMessages = invoice.SriMessages,
            SriEnvironment = ParseSriEnvironment(invoice.SriEnvironment),
            Items = invoice.Items.Select(item => new InvoiceItemDto
            {
                Id = item.Id,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent,
                Subtotal = item.Subtotal,
                TaxRate = item.TaxRate,
                Tax = item.Tax,
                Total = item.Total
            }).ToList()
        };
    }
}
