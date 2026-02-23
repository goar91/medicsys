using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Controllers;

[ApiController]
[Route("api/sri")]
[Authorize(Roles = Roles.Odontologo)]
public class SriAuthorizationController : ControllerBase
{
    private readonly OdontologoDbContext _db;
    private readonly ISriService _sri;
    private readonly ILogger<SriAuthorizationController> _logger;

    public SriAuthorizationController(
        OdontologoDbContext db, 
        ISriService sri, 
        ILogger<SriAuthorizationController> logger)
    {
        _db = db;
        _sri = sri;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IQueryable<Invoice> GetOwnedInvoices(Guid odontologoId)
    {
        return _db.Invoices.Where(i => _db.OdontologoInvoiceOwnerships
            .Any(o => o.InvoiceId == i.Id && o.OdontologoId == odontologoId));
    }

    /// <summary>
    /// Obtiene todas las facturas pendientes de envío al SRI
    /// </summary>
    [HttpGet("pending-invoices")]
    public async Task<ActionResult<IEnumerable<object>>> GetPendingInvoices()
    {
        var odontologoId = GetUserId();
        var pendingInvoices = await GetOwnedInvoices(odontologoId)
            .Where(x =>
                x.Status == InvoiceStatus.Pending ||
                x.Status == InvoiceStatus.Rejected ||
                x.Status == InvoiceStatus.AwaitingAuthorization)
            .OrderByDescending(x => x.IssuedAt)
            .Select(x => new
            {
                x.Id,
                x.Number,
                x.Sequential,
                x.IssuedAt,
                x.CustomerName,
                x.Total,
                x.TotalToCharge,
                Status = x.Status.ToString(),
                x.SriEnvironment,
                x.SriMessages,
                x.SriAccessKey,
                x.SriAuthorizationNumber
            })
            .ToListAsync();

        return Ok(pendingInvoices);
    }

    /// <summary>
    /// Obtiene todas las facturas autorizadas por el SRI
    /// </summary>
    [HttpGet("authorized-invoices")]
    public async Task<ActionResult<IEnumerable<object>>> GetAuthorizedInvoices(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var odontologoId = GetUserId();
        var query = GetOwnedInvoices(odontologoId)
            .Where(x => x.Status == InvoiceStatus.Authorized);

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
            query = query.Where(x => x.SriAuthorizedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            query = query.Where(x => x.SriAuthorizedAt <= toUtc);
        }

        var authorizedInvoices = await query
            .OrderByDescending(x => x.SriAuthorizedAt)
            .Select(x => new
            {
                x.Id,
                x.Number,
                x.Sequential,
                x.IssuedAt,
                x.CustomerName,
                x.Total,
                x.TotalToCharge,
                x.SriEnvironment,
                x.SriAccessKey,
                x.SriAuthorizationNumber,
                x.SriAuthorizedAt,
                x.SriMessages
            })
            .ToListAsync();

        return Ok(authorizedInvoices);
    }

    /// <summary>
    /// Envía una factura específica al SRI para autorización
    /// </summary>
    [HttpPost("send-invoice/{id:guid}")]
    public async Task<ActionResult<object>> SendInvoice(Guid id)
    {
        var odontologoId = GetUserId();
        var invoice = await GetOwnedInvoices(odontologoId)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (invoice == null)
        {
            return NotFound(new { message = "Factura no encontrada" });
        }

        if (invoice.Status == InvoiceStatus.Authorized)
        {
            return BadRequest(new { message = "La factura ya está autorizada" });
        }

        try
        {
            var result = await _sri.SendInvoiceAsync(invoice, invoice.SriEnvironment);
            ApplySriResultToInvoice(invoice, result);

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Factura {Number} enviada al SRI. Estado: {Status}", 
                invoice.Number, 
                invoice.Status);

            return Ok(new
            {
                invoice.Id,
                invoice.Number,
                Status = invoice.Status.ToString(),
                invoice.SriEnvironment,
                invoice.SriAccessKey,
                invoice.SriAuthorizationNumber,
                invoice.SriAuthorizedAt,
                invoice.SriMessages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar factura {Number} al SRI", invoice.Number);
            return StatusCode(500, new { message = "Error al enviar al SRI", error = ex.Message });
        }
    }

    /// <summary>
    /// Envía múltiples facturas al SRI en lote
    /// </summary>
    [HttpPost("send-batch")]
    public async Task<ActionResult<object>> SendBatch([FromBody] List<Guid> invoiceIds)
    {
        var odontologoId = GetUserId();
        if (invoiceIds == null || !invoiceIds.Any())
        {
            return BadRequest(new { message = "Debe proporcionar al menos una factura" });
        }

        var invoices = await GetOwnedInvoices(odontologoId)
            .Include(x => x.Items)
            .Where(x => invoiceIds.Contains(x.Id) && 
                       (x.Status == InvoiceStatus.Pending ||
                        x.Status == InvoiceStatus.Rejected ||
                        x.Status == InvoiceStatus.AwaitingAuthorization))
            .ToListAsync();

        var results = new List<object>();
        var successful = 0;
        var failed = 0;

        foreach (var invoice in invoices)
        {
            try
            {
                var result = await _sri.SendInvoiceAsync(invoice, invoice.SriEnvironment);
                ApplySriResultToInvoice(invoice, result);

                if (invoice.Status == InvoiceStatus.Authorized)
                    successful++;
                else
                    failed++;

                results.Add(new
                {
                    invoice.Id,
                    invoice.Number,
                    invoice.SriEnvironment,
                    Status = invoice.Status.ToString(),
                    Success = invoice.Status == InvoiceStatus.Authorized
                });
            }
            catch (Exception ex)
            {
                failed++;
                results.Add(new
                {
                    invoice.Id,
                    invoice.Number,
                    Status = "Error",
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Total = invoices.Count,
            Successful = successful,
            Failed = failed,
            Results = results
        });
    }

    /// <summary>
    /// Consulta el estado de autorización de una factura en el SRI
    /// </summary>
    [HttpGet("check-status/{id:guid}")]
    public async Task<ActionResult<object>> CheckAuthorizationStatus(Guid id)
    {
        var odontologoId = GetUserId();
        var invoice = await GetOwnedInvoices(odontologoId)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (invoice == null)
        {
            return NotFound(new { message = "Factura no encontrada" });
        }

        if (string.IsNullOrEmpty(invoice.SriAccessKey))
        {
            return Ok(new
            {
                invoice.Id,
                invoice.Number,
                Status = "NotSent",
                Message = "La factura aún no ha sido enviada al SRI"
            });
        }

        try
        {
            // Aquí podrías implementar una consulta real al SRI
            // Por ahora retornamos el estado actual de la base de datos
            return Ok(new
            {
                invoice.Id,
                invoice.Number,
                Status = invoice.Status.ToString(),
                invoice.SriEnvironment,
                invoice.SriAccessKey,
                invoice.SriAuthorizationNumber,
                invoice.SriAuthorizedAt,
                invoice.SriMessages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar estado de factura {Number}", invoice.Number);
            return StatusCode(500, new { message = "Error al consultar estado", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene estadísticas de autorización SRI
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetAuthorizationStats(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var odontologoId = GetUserId();
        var query = GetOwnedInvoices(odontologoId);

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
            query = query.Where(x => x.IssuedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            query = query.Where(x => x.IssuedAt <= toUtc);
        }

        var stats = await query
            .GroupBy(x => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Pending = g.Count(x => x.Status == InvoiceStatus.Pending || x.Status == InvoiceStatus.AwaitingAuthorization),
                AwaitingAuthorization = g.Count(x => x.Status == InvoiceStatus.AwaitingAuthorization),
                Authorized = g.Count(x => x.Status == InvoiceStatus.Authorized),
                Rejected = g.Count(x => x.Status == InvoiceStatus.Rejected),
                TotalAmount = g.Sum(x => x.Total),
                AuthorizedAmount = g.Where(x => x.Status == InvoiceStatus.Authorized).Sum(x => x.Total),
                PendingAmount = g.Where(x => x.Status == InvoiceStatus.Pending || x.Status == InvoiceStatus.AwaitingAuthorization).Sum(x => x.Total)
            })
            .FirstOrDefaultAsync();

        return Ok(stats ?? new
        {
            Total = 0,
            Pending = 0,
            AwaitingAuthorization = 0,
            Authorized = 0,
            Rejected = 0,
            TotalAmount = 0m,
            AuthorizedAmount = 0m,
            PendingAmount = 0m
        });
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
}
