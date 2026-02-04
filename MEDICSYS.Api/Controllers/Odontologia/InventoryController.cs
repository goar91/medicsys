using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Contracts;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models.Odontologia;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers.Odontologia;

[ApiController]
[Route("api/odontologia/inventory")]
[Authorize(Roles = Roles.Odontologo)]
public class InventoryController : ControllerBase
{
    private readonly OdontologoDbContext _db;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(OdontologoDbContext db, ILogger<InventoryController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Guid GetOdontologoId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAll()
    {
        var odontologoId = GetOdontologoId();
        var items = await _db.InventoryItems
            .Where(i => i.OdontologoId == odontologoId)
            .OrderBy(i => i.Name)
            .ToListAsync();

        return Ok(items.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InventoryItemDto>> GetById(Guid id)
    {
        var odontologoId = GetOdontologoId();
        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id && i.OdontologoId == odontologoId);

        if (item == null)
            return NotFound();

        return Ok(MapToDto(item));
    }

    [HttpPost]
    public async Task<ActionResult<InventoryItemDto>> Create(CreateInventoryItemRequest request)
    {
        var odontologoId = GetOdontologoId();

        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            OdontologoId = odontologoId,
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Quantity = request.Quantity,
            MinimumQuantity = request.MinimumQuantity,
            UnitPrice = request.UnitPrice,
            Supplier = request.Supplier,
            ExpirationDate = request.ExpirationDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();

        // Verificar si se deben crear alertas
        await CheckAndCreateAlertsAsync(item);

        _logger.LogInformation("Inventory item {ItemName} created for Odontologo {OdontologoId}", item.Name, odontologoId);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, MapToDto(item));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<InventoryItemDto>> Update(Guid id, UpdateInventoryItemRequest request)
    {
        var odontologoId = GetOdontologoId();
        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id && i.OdontologoId == odontologoId);

        if (item == null)
            return NotFound();

        item.Name = request.Name;
        item.Description = request.Description;
        item.Sku = request.Sku;
        item.Quantity = request.Quantity;
        item.MinimumQuantity = request.MinimumQuantity;
        item.UnitPrice = request.UnitPrice;
        item.Supplier = request.Supplier;
        item.ExpirationDate = request.ExpirationDate;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Verificar si se deben crear o resolver alertas
        await CheckAndCreateAlertsAsync(item);

        _logger.LogInformation("Inventory item {ItemId} updated for Odontologo {OdontologoId}", id, odontologoId);
        return Ok(MapToDto(item));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var odontologoId = GetOdontologoId();
        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id && i.OdontologoId == odontologoId);

        if (item == null)
            return NotFound();

        _db.InventoryItems.Remove(item);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Inventory item {ItemId} deleted for Odontologo {OdontologoId}", id, odontologoId);
        return NoContent();
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<IEnumerable<InventoryAlertDto>>> GetAlerts([FromQuery] bool? resolved = null)
    {
        var odontologoId = GetOdontologoId();
        var query = _db.InventoryAlerts
            .Include(a => a.InventoryItem)
            .Where(a => a.OdontologoId == odontologoId);

        if (resolved.HasValue)
            query = query.Where(a => a.IsResolved == resolved.Value);

        var alerts = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Ok(alerts.Select(MapAlertToDto));
    }

    [HttpPost("alerts/{alertId}/resolve")]
    public async Task<IActionResult> ResolveAlert(Guid alertId)
    {
        var odontologoId = GetOdontologoId();
        var alert = await _db.InventoryAlerts
            .FirstOrDefaultAsync(a => a.Id == alertId && a.OdontologoId == odontologoId);

        if (alert == null)
            return NotFound();

        alert.IsResolved = true;
        alert.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Alert {AlertId} resolved for Odontologo {OdontologoId}", alertId, odontologoId);
        return NoContent();
    }

    [HttpPost("check-alerts")]
    public async Task<IActionResult> CheckAllAlerts()
    {
        var odontologoId = GetOdontologoId();
        var items = await _db.InventoryItems
            .Where(i => i.OdontologoId == odontologoId)
            .ToListAsync();

        foreach (var item in items)
        {
            await CheckAndCreateAlertsAsync(item);
        }

        return NoContent();
    }

    private async Task CheckAndCreateAlertsAsync(InventoryItem item)
    {
        // Resolver alertas viejas si el problema ya no existe
        var existingAlerts = await _db.InventoryAlerts
            .Where(a => a.InventoryItemId == item.Id && !a.IsResolved)
            .ToListAsync();

        // Resolver alerta de stock bajo/agotado si ahora hay suficiente stock
        if (item.Quantity > item.MinimumQuantity)
        {
            var stockAlerts = existingAlerts
                .Where(a => a.Type == AlertType.LowStock || a.Type == AlertType.OutOfStock)
                .ToList();
            
            foreach (var alert in stockAlerts)
            {
                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
            }
        }

        // Crear alertas nuevas si es necesario
        var alertsToCreate = new List<InventoryAlert>();

        // Alerta de stock agotado
        if (item.Quantity == 0 && !existingAlerts.Any(a => a.Type == AlertType.OutOfStock && !a.IsResolved))
        {
            alertsToCreate.Add(new InventoryAlert
            {
                Id = Guid.NewGuid(),
                InventoryItemId = item.Id,
                OdontologoId = item.OdontologoId,
                Type = AlertType.OutOfStock,
                Message = $"El artículo '{item.Name}' está agotado. Stock: 0",
                CreatedAt = DateTime.UtcNow
            });
        }
        // Alerta de stock bajo
        else if (item.IsLowStock && !existingAlerts.Any(a => a.Type == AlertType.LowStock && !a.IsResolved))
        {
            alertsToCreate.Add(new InventoryAlert
            {
                Id = Guid.NewGuid(),
                InventoryItemId = item.Id,
                OdontologoId = item.OdontologoId,
                Type = AlertType.LowStock,
                Message = $"Stock bajo de '{item.Name}'. Stock actual: {item.Quantity}, Mínimo: {item.MinimumQuantity}",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Alerta de expiración
        if (item.ExpirationDate.HasValue)
        {
            if (item.ExpirationDate.Value <= DateTime.UtcNow && !existingAlerts.Any(a => a.Type == AlertType.Expired && !a.IsResolved))
            {
                alertsToCreate.Add(new InventoryAlert
                {
                    Id = Guid.NewGuid(),
                    InventoryItemId = item.Id,
                    OdontologoId = item.OdontologoId,
                    Type = AlertType.Expired,
                    Message = $"El artículo '{item.Name}' expiró el {item.ExpirationDate.Value:dd/MM/yyyy}",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (item.IsExpiringSoon && !existingAlerts.Any(a => a.Type == AlertType.ExpirationWarning && !a.IsResolved))
            {
                var daysUntilExpiration = (item.ExpirationDate.Value - DateTime.UtcNow).Days;
                alertsToCreate.Add(new InventoryAlert
                {
                    Id = Guid.NewGuid(),
                    InventoryItemId = item.Id,
                    OdontologoId = item.OdontologoId,
                    Type = AlertType.ExpirationWarning,
                    Message = $"El artículo '{item.Name}' expirará en {daysUntilExpiration} días ({item.ExpirationDate.Value:dd/MM/yyyy})",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (alertsToCreate.Any())
        {
            _db.InventoryAlerts.AddRange(alertsToCreate);
            await _db.SaveChangesAsync();
        }
    }

    private static InventoryItemDto MapToDto(InventoryItem item) => new(
        item.Id,
        item.Name,
        item.Description,
        item.Sku,
        item.Quantity,
        item.MinimumQuantity,
        item.UnitPrice,
        item.Supplier,
        item.ExpirationDate,
        item.IsLowStock,
        item.IsExpiringSoon,
        item.CreatedAt,
        item.UpdatedAt
    );

    private static InventoryAlertDto MapAlertToDto(InventoryAlert alert) => new(
        alert.Id,
        alert.InventoryItemId,
        alert.Type.ToString(),
        alert.Message,
        alert.IsResolved,
        alert.CreatedAt,
        alert.ResolvedAt,
        alert.InventoryItem != null ? MapToDto(alert.InventoryItem) : null
    );
}
