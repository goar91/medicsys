using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
    private readonly IRideService _ride;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(OdontologoDbContext db, ISriService sri, IRideService ride, ILogger<InvoicesController> logger)
    {
        _db = db;
        _sri = sri;
        _ride = ride;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IQueryable<Invoice> GetOwnedInvoices(Guid odontologoId)
    {
        return _db.Invoices.Where(i => _db.OdontologoInvoiceOwnerships
            .Any(o => o.InvoiceId == i.Id && o.OdontologoId == odontologoId));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        var odontologoId = GetUserId();

        var query = GetOwnedInvoices(odontologoId)
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
        var odontologoId = GetUserId();
        var invoice = await GetOwnedInvoices(odontologoId)
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (invoice == null)
        {
            return NotFound();
        }

        return Ok(Map(invoice));
    }

    [HttpGet("{id:guid}/ride")]
    public async Task<IActionResult> GetRide(Guid id)
    {
        var odontologoId = GetUserId();
        var invoice = await GetOwnedInvoices(odontologoId)
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (invoice == null)
        {
            return NotFound();
        }

        try
        {
            var pdf = _ride.GenerateRide(invoice);
            var fileName = $"RIDE-{invoice.Number.Replace("-", "")}.pdf";
            return File(pdf, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar RIDE para factura {InvoiceId}", id);
            return StatusCode(500, "Error al generar el RIDE del comprobante.");
        }
    }

    [HttpGet("config")]
    public async Task<ActionResult<InvoiceConfigDto>> GetConfig()
    {
        var odontologoId = GetUserId();
        var config = await _db.InvoiceConfigs.FirstOrDefaultAsync();

        if (config == null)
        {
            config = new InvoiceConfig
            {
                Id = Guid.NewGuid(),
                EstablishmentCode = "001",
                EmissionPoint = "002",
                UpdatedAt = DateTimeHelper.Now()
            };
            _db.InvoiceConfigs.Add(config);
            await _db.SaveChangesAsync();
        }

        var nextSeq = (await GetOwnedInvoices(odontologoId).MaxAsync(x => (int?)x.Sequential) ?? 0) + 1;

        return Ok(new InvoiceConfigDto
        {
            EstablishmentCode = config.EstablishmentCode,
            EmissionPoint = config.EmissionPoint,
            NextSequential = nextSeq,
            NextNumber = FormatInvoiceNumber(config.EstablishmentCode, config.EmissionPoint, nextSeq)
        });
    }

    [HttpPut("config")]
    public async Task<ActionResult<InvoiceConfigDto>> UpdateConfig(InvoiceConfigUpdateRequest request)
    {
        var odontologoId = GetUserId();
        var config = await _db.InvoiceConfigs.FirstOrDefaultAsync();

        if (config == null)
        {
            config = new InvoiceConfig
            {
                Id = Guid.NewGuid(),
                EstablishmentCode = request.EstablishmentCode.PadLeft(3, '0'),
                EmissionPoint = request.EmissionPoint.PadLeft(3, '0'),
                UpdatedAt = DateTimeHelper.Now()
            };
            _db.InvoiceConfigs.Add(config);
        }
        else
        {
            config.EstablishmentCode = request.EstablishmentCode.PadLeft(3, '0');
            config.EmissionPoint = request.EmissionPoint.PadLeft(3, '0');
            config.UpdatedAt = DateTimeHelper.Now();
        }

        await _db.SaveChangesAsync();

        var nextSeq = (await GetOwnedInvoices(odontologoId).MaxAsync(x => (int?)x.Sequential) ?? 0) + 1;

        return Ok(new InvoiceConfigDto
        {
            EstablishmentCode = config.EstablishmentCode,
            EmissionPoint = config.EmissionPoint,
            NextSequential = nextSeq,
            NextNumber = FormatInvoiceNumber(config.EstablishmentCode, config.EmissionPoint, nextSeq)
        });
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(InvoiceCreateRequest request)
    {
        var odontologoId = GetUserId();

        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest("Debe incluir al menos un item.");
        }

        var method = ParsePaymentMethod(request.PaymentMethod);

        // Load invoice config for establishment code and emission point
        var config = await _db.InvoiceConfigs.FirstOrDefaultAsync();
        var establishment = config?.EstablishmentCode ?? "001";
        var emissionPoint = config?.EmissionPoint ?? "002";

        var nextSequential = (await GetOwnedInvoices(odontologoId).MaxAsync(x => (int?)x.Sequential) ?? 0) + 1;
        var issuedAt = DateTimeHelper.Now();
        var number = FormatInvoiceNumber(establishment, emissionPoint, nextSequential);

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
            EstablishmentCode = establishment,
            EmissionPoint = emissionPoint,
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
        _db.OdontologoInvoiceOwnerships.Add(new Models.Odontologia.OdontologoInvoiceOwnership
        {
            InvoiceId = invoice.Id,
            OdontologoId = odontologoId,
            CreatedAt = DateTimeHelper.Now()
        });
        await _db.SaveChangesAsync();

        await RegisterAccountingEntryAsync(invoice, odontologoId);

        if (request.SendToSri)
        {
            await SendToSriInternalAsync(invoice);
        }

        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, Map(invoice));
    }

    [HttpPost("{id:guid}/send-sri")]
    public async Task<ActionResult<InvoiceDto>> SendToSri(Guid id)
    {
        var odontologoId = GetUserId();
        var invoice = await GetOwnedInvoices(odontologoId)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (invoice == null)
        {
            return NotFound();
        }

        await SendToSriInternalAsync(invoice);
        return Ok(Map(invoice));
    }

    [HttpGet("awaiting-authorization")]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAwaitingAuthorization(
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        var odontologoId = GetUserId();

        var query = GetOwnedInvoices(odontologoId)
            .Include(x => x.Items)
            .AsNoTracking()
            .Where(x =>
                x.Status == InvoiceStatus.AwaitingAuthorization ||
                (x.Status == InvoiceStatus.Pending &&
                 x.SriAccessKey != null &&
                 x.SriAccessKey != string.Empty &&
                 x.SriAuthorizationNumber == null));

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

    [HttpPost("send-awaiting-sri")]
    public async Task<ActionResult<object>> SendAwaitingToSri()
    {
        var odontologoId = GetUserId();
        var invoices = await GetOwnedInvoices(odontologoId)
            .Include(x => x.Items)
            .Where(x =>
                x.Status == InvoiceStatus.AwaitingAuthorization ||
                (x.Status == InvoiceStatus.Pending &&
                 x.SriAccessKey != null &&
                 x.SriAccessKey != string.Empty &&
                 x.SriAuthorizationNumber == null))
            .OrderBy(x => x.IssuedAt)
            .ToListAsync();

        var results = new List<object>();
        var authorized = 0;
        var rejected = 0;
        var awaiting = 0;
        var pending = 0;
        var errors = 0;

        foreach (var invoice in invoices)
        {
            try
            {
                var result = await _sri.SendInvoiceAsync(invoice, invoice.SriEnvironment);
                ApplySriResultToInvoice(invoice, result);

                switch (invoice.Status)
                {
                    case InvoiceStatus.Authorized:
                        authorized++;
                        break;
                    case InvoiceStatus.Rejected:
                        rejected++;
                        break;
                    case InvoiceStatus.AwaitingAuthorization:
                        awaiting++;
                        break;
                    default:
                        pending++;
                        break;
                }

                results.Add(new
                {
                    invoice.Id,
                    invoice.Number,
                    Status = invoice.Status.ToString(),
                    invoice.SriEnvironment,
                    invoice.SriAccessKey,
                    invoice.SriAuthorizationNumber,
                    invoice.SriMessages
                });
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogError(ex, "Error al reenviar factura {Number} al SRI", invoice.Number);
                results.Add(new
                {
                    invoice.Id,
                    invoice.Number,
                    Status = "Error",
                    Error = ex.Message
                });
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Total = invoices.Count,
            Authorized = authorized,
            Rejected = rejected,
            AwaitingAuthorization = awaiting,
            Pending = pending,
            Errors = errors,
            Results = results
        });
    }

    private async Task SendToSriInternalAsync(Invoice invoice)
    {
        var result = await _sri.SendInvoiceAsync(invoice, invoice.SriEnvironment);
        ApplySriResultToInvoice(invoice, result);

        await _db.SaveChangesAsync();
    }

    private static void ApplySriResultToInvoice(Invoice invoice, SriSendResult result)
    {
        invoice.SriAccessKey = result.AccessKey;
        invoice.SriAuthorizationNumber = result.AuthorizationNumber;
        invoice.SriAuthorizedAt = result.AuthorizedAt;
        invoice.SriMessages = result.Messages;
        invoice.Status = MapSriStatusToInvoiceStatus(result.Status);
        invoice.UpdatedAt = DateTimeHelper.Now();
    }

    private static InvoiceStatus MapSriStatusToInvoiceStatus(string? sriStatus)
    {
        return sriStatus?.ToUpperInvariant() switch
        {
            "AUTORIZADO" => InvoiceStatus.Authorized,
            "RECHAZADO" => InvoiceStatus.Rejected,
            "NO AUTORIZADO" => InvoiceStatus.Rejected,
            "ERROR" => InvoiceStatus.Rejected,
            "EN_ESPERA_AUTORIZACION" => InvoiceStatus.AwaitingAuthorization,
            "EN PROCESO" => InvoiceStatus.AwaitingAuthorization,
            _ => InvoiceStatus.Pending
        };
    }

    private async Task RegisterAccountingEntryAsync(Invoice invoice, Guid odontologoId)
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
            CreatedAt = DateTimeHelper.Now()
        };

        _db.AccountingEntries.Add(entry);
        _db.OdontologoAccountingEntryOwnerships.Add(new Models.Odontologia.OdontologoAccountingEntryOwnership
        {
            AccountingEntryId = entry.Id,
            OdontologoId = odontologoId,
            CreatedAt = DateTimeHelper.Now()
        });
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

    private static string FormatInvoiceNumber(string establishment, string emissionPoint, int sequential)
    {
        return $"{establishment.PadLeft(3, '0')}-{emissionPoint.PadLeft(3, '0')}-{sequential.ToString().PadLeft(9, '0')}";
    }

    private static InvoiceDto Map(Invoice invoice)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            Number = invoice.Number,
            Sequential = invoice.Sequential,
            EstablishmentCode = invoice.EstablishmentCode,
            EmissionPoint = invoice.EmissionPoint,
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
