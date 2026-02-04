using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models.Odontologia;
using System.Security.Claims;

namespace MEDICSYS.Api.Controllers.Odontologia;

[Authorize(Roles = "Odontologo")]
[ApiController]
[Route("api/odontologia/kardex")]
public class KardexController : ControllerBase
{
    private readonly OdontologoDbContext _db;
    private readonly ILogger<KardexController> _logger;

    public KardexController(OdontologoDbContext db, ILogger<KardexController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Guid GetOdontologoId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("items")]
    public async Task<ActionResult<List<object>>> GetInventoryItems()
    {
        var odontologoId = GetOdontologoId();

        var items = await _db.InventoryItems
            .Where(i => i.OdontologoId == odontologoId)
            .OrderBy(i => i.Name)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Description,
                i.Sku,
                i.Quantity,
                i.MinimumQuantity,
                i.MaximumQuantity,
                i.ReorderPoint,
                i.UnitPrice,
                i.AverageCost,
                i.Supplier,
                i.Location,
                i.Batch,
                i.ExpirationDate,
                i.IsLowStock,
                i.IsExpiringSoon,
                i.NeedsReorder,
                i.CreatedAt,
                i.UpdatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("items/{id}")]
    public async Task<ActionResult<object>> GetInventoryItem(Guid id)
    {
        var odontologoId = GetOdontologoId();

        var item = await _db.InventoryItems
            .Where(i => i.Id == id && i.OdontologoId == odontologoId)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Description,
                i.Sku,
                i.Quantity,
                i.MinimumQuantity,
                i.MaximumQuantity,
                i.ReorderPoint,
                i.UnitPrice,
                i.AverageCost,
                i.Supplier,
                i.Location,
                i.Batch,
                i.ExpirationDate,
                i.IsLowStock,
                i.IsExpiringSoon,
                i.NeedsReorder,
                i.CreatedAt,
                i.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [HttpGet("movements")]
    public async Task<ActionResult<List<object>>> GetMovements(
        [FromQuery] Guid? inventoryItemId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? movementType)
    {
        var odontologoId = GetOdontologoId();

        var query = _db.InventoryMovements
            .Include(m => m.InventoryItem)
            .Where(m => m.OdontologoId == odontologoId);

        if (inventoryItemId.HasValue)
            query = query.Where(m => m.InventoryItemId == inventoryItemId.Value);

        if (startDate.HasValue)
            query = query.Where(m => m.MovementDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(m => m.MovementDate <= endDate.Value);

        if (!string.IsNullOrEmpty(movementType))
            query = query.Where(m => m.MovementType == movementType);

        var movements = await query
            .OrderByDescending(m => m.MovementDate)
            .Select(m => new
            {
                m.Id,
                m.InventoryItemId,
                InventoryItemName = m.InventoryItem != null ? m.InventoryItem.Name : "",
                m.MovementDate,
                m.MovementType,
                m.Quantity,
                m.UnitPrice,
                m.TotalCost,
                m.StockBefore,
                m.StockAfter,
                m.Reference,
                m.Notes,
                m.CreatedAt
            })
            .ToListAsync();

        return Ok(movements);
    }

    [HttpPost("movements/entry")]
    public async Task<ActionResult<object>> AddEntry([FromBody] MovementRequest request)
    {
        var odontologoId = GetOdontologoId();

        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId && i.OdontologoId == odontologoId);

        if (item == null)
            return NotFound("Item not found");

        var stockBefore = item.Quantity;
        var stockAfter = stockBefore + request.Quantity;

        // Calculate new average cost (weighted average)
        var totalValue = (item.AverageCost ?? item.UnitPrice) * item.Quantity + request.UnitPrice * request.Quantity;
        var totalQuantity = item.Quantity + request.Quantity;
        var newAverageCost = totalQuantity > 0 ? totalValue / totalQuantity : request.UnitPrice;

        var movement = new InventoryMovement
        {
            OdontologoId = odontologoId,
            InventoryItemId = request.InventoryItemId,
            MovementDate = DateTime.SpecifyKind(request.MovementDate ?? DateTime.UtcNow, DateTimeKind.Utc),
            MovementType = "Entry",
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            TotalCost = request.Quantity * request.UnitPrice,
            StockBefore = stockBefore,
            StockAfter = stockAfter,
            Reference = request.Reference,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        item.Quantity = stockAfter;
        item.AverageCost = newAverageCost;
        item.UpdatedAt = DateTime.UtcNow;

        _db.InventoryMovements.Add(movement);
        await _db.SaveChangesAsync();

        return Ok(new { movement, item });
    }

    [HttpPost("movements/exit")]
    public async Task<ActionResult<object>> AddExit([FromBody] MovementRequest request)
    {
        var odontologoId = GetOdontologoId();

        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId && i.OdontologoId == odontologoId);

        if (item == null)
            return NotFound("Item not found");

        if (item.Quantity < request.Quantity)
            return BadRequest("Insufficient stock");

        var stockBefore = item.Quantity;
        var stockAfter = stockBefore - request.Quantity;

        var movement = new InventoryMovement
        {
            OdontologoId = odontologoId,
            InventoryItemId = request.InventoryItemId,
            MovementDate = DateTime.SpecifyKind(request.MovementDate ?? DateTime.UtcNow, DateTimeKind.Utc),
            MovementType = "Exit",
            Quantity = request.Quantity,
            UnitPrice = item.AverageCost ?? item.UnitPrice,
            TotalCost = request.Quantity * (item.AverageCost ?? item.UnitPrice),
            StockBefore = stockBefore,
            StockAfter = stockAfter,
            Reference = request.Reference,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        item.Quantity = stockAfter;
        item.UpdatedAt = DateTime.UtcNow;

        _db.InventoryMovements.Add(movement);
        await _db.SaveChangesAsync();

        return Ok(new { movement, item });
    }

    [HttpPost("movements/adjustment")]
    public async Task<ActionResult<object>> AddAdjustment([FromBody] AdjustmentRequest request)
    {
        var odontologoId = GetOdontologoId();

        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId && i.OdontologoId == odontologoId);

        if (item == null)
            return NotFound("Item not found");

        var stockBefore = item.Quantity;
        var stockAfter = request.NewQuantity;
        var difference = stockAfter - stockBefore;

        var movement = new InventoryMovement
        {
            OdontologoId = odontologoId,
            InventoryItemId = request.InventoryItemId,
            MovementDate = DateTime.SpecifyKind(request.MovementDate ?? DateTime.UtcNow, DateTimeKind.Utc),
            MovementType = "Adjustment",
            Quantity = Math.Abs(difference),
            UnitPrice = item.AverageCost ?? item.UnitPrice,
            TotalCost = Math.Abs(difference) * (item.AverageCost ?? item.UnitPrice),
            StockBefore = stockBefore,
            StockAfter = stockAfter,
            Reference = request.Reason,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        item.Quantity = stockAfter;
        item.UpdatedAt = DateTime.UtcNow;

        _db.InventoryMovements.Add(movement);
        await _db.SaveChangesAsync();

        return Ok(new { movement, item });
    }

    [HttpPost("items")]
    public async Task<ActionResult<object>> CreateItem([FromBody] CreateItemRequest request)
    {
        var odontologoId = GetOdontologoId();

        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            OdontologoId = odontologoId,
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Quantity = request.InitialQuantity ?? 0,
            MinimumQuantity = request.MinimumQuantity,
            MaximumQuantity = request.MaximumQuantity,
            ReorderPoint = request.ReorderPoint,
            UnitPrice = request.UnitPrice,
            AverageCost = request.UnitPrice,
            Supplier = request.Supplier,
            Location = request.Location,
            Batch = request.Batch,
            ExpirationDate = request.ExpirationDate.HasValue 
                ? DateTime.SpecifyKind(request.ExpirationDate.Value, DateTimeKind.Utc) 
                : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetInventoryItem), new { id = item.Id }, new
        {
            item.Id,
            item.Name,
            item.Description,
            item.Sku,
            item.Quantity,
            item.MinimumQuantity,
            item.MaximumQuantity,
            item.ReorderPoint,
            item.UnitPrice,
            item.AverageCost,
            item.Supplier,
            item.Location,
            item.Batch,
            item.ExpirationDate,
            item.IsLowStock,
            item.IsExpiringSoon,
            item.NeedsReorder,
            item.CreatedAt,
            item.UpdatedAt
        });
    }

    [HttpPut("items/{id}")]
    public async Task<ActionResult<object>> UpdateItem(Guid id, [FromBody] UpdateItemRequest request)
    {
        var odontologoId = GetOdontologoId();

        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == id && i.OdontologoId == odontologoId);

        if (item == null)
            return NotFound();

        item.Name = request.Name;
        item.Description = request.Description;
        item.Sku = request.Sku;
        item.MinimumQuantity = request.MinimumQuantity;
        item.MaximumQuantity = request.MaximumQuantity;
        item.ReorderPoint = request.ReorderPoint;
        item.UnitPrice = request.UnitPrice;
        item.Supplier = request.Supplier;
        item.Location = request.Location;
        item.Batch = request.Batch;
        item.ExpirationDate = request.ExpirationDate.HasValue 
            ? DateTime.SpecifyKind(request.ExpirationDate.Value, DateTimeKind.Utc) 
            : null;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(item);
    }

    [HttpGet("kardex/{inventoryItemId}")]
    public async Task<ActionResult<object>> GetKardex(Guid inventoryItemId)
    {
        var odontologoId = GetOdontologoId();

        var item = await _db.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == inventoryItemId && i.OdontologoId == odontologoId);

        if (item == null)
            return NotFound();

        var movements = await _db.InventoryMovements
            .Where(m => m.InventoryItemId == inventoryItemId)
            .OrderBy(m => m.MovementDate)
            .Select(m => new
            {
                m.Id,
                m.MovementDate,
                m.MovementType,
                m.Quantity,
                m.UnitPrice,
                m.TotalCost,
                m.StockBefore,
                m.StockAfter,
                m.Reference,
                m.Notes
            })
            .ToListAsync();

        return Ok(new
        {
            Item = new
            {
                item.Id,
                item.Name,
                item.Sku,
                item.Quantity,
                item.MinimumQuantity,
                item.MaximumQuantity,
                item.ReorderPoint,
                item.UnitPrice,
                item.AverageCost,
                item.Supplier,
                item.Location,
                item.Batch
            },
            Movements = movements,
            Summary = new
            {
                TotalEntries = movements.Where(m => m.MovementType == "Entry").Sum(m => m.Quantity),
                TotalExits = movements.Where(m => m.MovementType == "Exit").Sum(m => m.Quantity),
                TotalAdjustments = movements.Where(m => m.MovementType == "Adjustment").Sum(m => m.Quantity),
                CurrentStock = item.Quantity,
                AverageCost = item.AverageCost
            }
        });
    }
}

public record MovementRequest(
    Guid InventoryItemId,
    int Quantity,
    decimal UnitPrice,
    DateTime? MovementDate,
    string? Reference,
    string? Notes
);

public record AdjustmentRequest(
    Guid InventoryItemId,
    int NewQuantity,
    string Reason,
    DateTime? MovementDate,
    string? Notes
);

public record CreateItemRequest(
    string Name,
    string? Description,
    string? Sku,
    int? InitialQuantity,
    int MinimumQuantity,
    int? MaximumQuantity,
    int? ReorderPoint,
    decimal UnitPrice,
    string? Supplier,
    string? Location,
    string? Batch,
    DateTime? ExpirationDate
);

public record UpdateItemRequest(
    string Name,
    string? Description,
    string? Sku,
    int MinimumQuantity,
    int? MaximumQuantity,
    int? ReorderPoint,
    decimal UnitPrice,
    string? Supplier,
    string? Location,
    string? Batch,
    DateTime? ExpirationDate
);
