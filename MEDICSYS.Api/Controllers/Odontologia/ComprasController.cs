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
[Route("api/odontologia/compras")]
[Authorize(Roles = Roles.Odontologo)]
public class ComprasController : ControllerBase
{
    private readonly OdontologoDbContext _db;
    private readonly ILogger<ComprasController> _logger;

    public ComprasController(OdontologoDbContext db, ILogger<ComprasController> logger)
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
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetAll(
        [FromQuery] string? supplier,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
    {
        var odontologoId = GetOdontologoId();
        var query = _db.PurchaseOrders
            .Include(p => p.Items)
            .ThenInclude(i => i.InventoryItem)
            .Where(p => p.OdontologoId == odontologoId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(supplier))
        {
            query = query.Where(p => p.Supplier.Contains(supplier));
        }

        if (dateFrom.HasValue)
        {
            var dateFromUtc = DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.PurchaseDate >= dateFromUtc);
        }

        if (dateTo.HasValue)
        {
            var dateToUtc = DateTime.SpecifyKind(dateTo.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(p => p.PurchaseDate < dateToUtc);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            if (Enum.TryParse<PurchaseStatus>(status, out var statusEnum))
            {
                query = query.Where(p => p.Status == statusEnum);
            }
        }

        var total = await query.CountAsync();

        if (page.HasValue || pageSize.HasValue)
        {
            var pageValue = Math.Max(1, page ?? 1);
            var sizeValue = Math.Clamp(pageSize ?? 50, 1, 200);
            query = query
                .OrderByDescending(p => p.PurchaseDate)
                .Skip((pageValue - 1) * sizeValue)
                .Take(sizeValue);

            Response.Headers["X-Total-Count"] = total.ToString();
            Response.Headers["X-Page"] = pageValue.ToString();
            Response.Headers["X-Page-Size"] = sizeValue.ToString();
        }
        else
        {
            query = query.OrderByDescending(p => p.PurchaseDate);
        }

        var purchases = await query.ToListAsync();

        return Ok(purchases.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(Guid id)
    {
        var odontologoId = GetOdontologoId();
        var purchase = await _db.PurchaseOrders
            .Include(p => p.Items)
            .ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(p => p.Id == id && p.OdontologoId == odontologoId);

        if (purchase == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(purchase));
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create([FromBody] CreatePurchaseOrderRequest request)
    {
        var odontologoId = GetOdontologoId();

        // Validar que los artículos existan
        var itemIds = request.Items.Select(i => i.InventoryItemId).ToList();
        var inventoryItemsList = await _db.InventoryItems
            .Where(i => itemIds.Contains(i.Id) && i.OdontologoId == odontologoId)
            .ToListAsync();
        var inventoryItems = inventoryItemsList.ToDictionary(i => i.Id);

        if (inventoryItems.Count != itemIds.Count)
        {
            return BadRequest("Uno o más artículos del inventario no existen");
        }

        var purchaseDate = DateTime.SpecifyKind(request.PurchaseDate, DateTimeKind.Utc);
        var now = DateTime.UtcNow;

        var purchase = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            OdontologoId = odontologoId,
            Supplier = request.Supplier,
            InvoiceNumber = request.InvoiceNumber,
            PurchaseDate = purchaseDate,
            Notes = request.Notes,
            Status = Enum.Parse<PurchaseStatus>(request.Status),
            CreatedAt = now,
            UpdatedAt = now
        };

        // Agregar items
        decimal total = 0;
        foreach (var itemReq in request.Items)
        {
            var item = new PurchaseItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = purchase.Id,
                InventoryItemId = itemReq.InventoryItemId,
                Quantity = itemReq.Quantity,
                UnitPrice = itemReq.UnitPrice,
                ExpirationDate = itemReq.ExpirationDate.HasValue 
                    ? DateTime.SpecifyKind(itemReq.ExpirationDate.Value, DateTimeKind.Utc) 
                    : null
            };

            total += item.Quantity * item.UnitPrice;
            purchase.Items.Add(item);

            // Si el estado es "Received", actualizar inventario
            if (purchase.Status == PurchaseStatus.Received)
            {
                var inventoryItem = inventoryItems[itemReq.InventoryItemId];
                inventoryItem.Quantity += itemReq.Quantity;
                inventoryItem.UpdatedAt = now;

                // Actualizar precio de compra si es diferente
                if (itemReq.UnitPrice > 0)
                {
                    inventoryItem.UnitPrice = itemReq.UnitPrice;
                }

                // Actualizar fecha de vencimiento si se proporcionó
                if (itemReq.ExpirationDate.HasValue)
                {
                    inventoryItem.ExpirationDate = DateTime.SpecifyKind(itemReq.ExpirationDate.Value, DateTimeKind.Utc);
                }
            }
        }

        purchase.Total = total;

        _db.PurchaseOrders.Add(purchase);
        await _db.SaveChangesAsync();

        // Cargar los datos relacionados para el DTO
        await _db.Entry(purchase)
            .Collection(p => p.Items)
            .Query()
            .Include(i => i.InventoryItem)
            .LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = purchase.Id }, MapToDto(purchase));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PurchaseOrderDto>> Update(Guid id, [FromBody] CreatePurchaseOrderRequest request)
    {
        var odontologoId = GetOdontologoId();
        var purchase = await _db.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && p.OdontologoId == odontologoId);

        if (purchase == null)
        {
            return NotFound();
        }

        // Si ya fue recibida, no se puede modificar
        if (purchase.Status == PurchaseStatus.Received)
        {
            return BadRequest("No se puede modificar una compra ya recibida");
        }

        purchase.Supplier = request.Supplier;
        purchase.InvoiceNumber = request.InvoiceNumber;
        purchase.PurchaseDate = DateTime.SpecifyKind(request.PurchaseDate, DateTimeKind.Utc);
        purchase.Notes = request.Notes;
        purchase.UpdatedAt = DateTime.UtcNow;

        // Actualizar items (simplificado: eliminar y recrear)
        _db.PurchaseItems.RemoveRange(purchase.Items);

        decimal total = 0;
        foreach (var itemReq in request.Items)
        {
            var item = new PurchaseItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = purchase.Id,
                InventoryItemId = itemReq.InventoryItemId,
                Quantity = itemReq.Quantity,
                UnitPrice = itemReq.UnitPrice,
                ExpirationDate = itemReq.ExpirationDate.HasValue 
                    ? DateTime.SpecifyKind(itemReq.ExpirationDate.Value, DateTimeKind.Utc) 
                    : null
            };

            total += item.Quantity * item.UnitPrice;
            purchase.Items.Add(item);
        }

        purchase.Total = total;

        await _db.SaveChangesAsync();

        // Recargar datos
        await _db.Entry(purchase)
            .Collection(p => p.Items)
            .Query()
            .Include(i => i.InventoryItem)
            .LoadAsync();

        return Ok(MapToDto(purchase));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var odontologoId = GetOdontologoId();
        var purchase = await _db.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == id && p.OdontologoId == odontologoId);

        if (purchase == null)
        {
            return NotFound();
        }

        // Si ya fue recibida, no se puede eliminar
        if (purchase.Status == PurchaseStatus.Received)
        {
            return BadRequest("No se puede eliminar una compra ya recibida. El inventario ya fue actualizado.");
        }

        _db.PurchaseOrders.Remove(purchase);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/receive")]
    public async Task<ActionResult<PurchaseOrderDto>> ReceivePurchase(Guid id)
    {
        var odontologoId = GetOdontologoId();
        var purchase = await _db.PurchaseOrders
            .Include(p => p.Items)
            .ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(p => p.Id == id && p.OdontologoId == odontologoId);

        if (purchase == null)
        {
            return NotFound();
        }

        if (purchase.Status == PurchaseStatus.Received)
        {
            return BadRequest("Esta compra ya fue recibida");
        }

        var now = DateTime.UtcNow;
        purchase.Status = PurchaseStatus.Received;
        purchase.UpdatedAt = now;

        // Actualizar inventario
        foreach (var item in purchase.Items)
        {
            if (item.InventoryItem != null)
            {
                item.InventoryItem.Quantity += item.Quantity;
                item.InventoryItem.UpdatedAt = now;

                if (item.UnitPrice > 0)
                {
                    item.InventoryItem.UnitPrice = item.UnitPrice;
                }

                if (item.ExpirationDate.HasValue)
                {
                    item.InventoryItem.ExpirationDate = item.ExpirationDate;
                }
            }
        }

        await _db.SaveChangesAsync();

        return Ok(MapToDto(purchase));
    }

    private static PurchaseOrderDto MapToDto(PurchaseOrder purchase)
    {
        return new PurchaseOrderDto
        {
            Id = purchase.Id,
            Supplier = purchase.Supplier,
            InvoiceNumber = purchase.InvoiceNumber,
            PurchaseDate = purchase.PurchaseDate,
            Notes = purchase.Notes,
            Total = purchase.Total,
            Status = purchase.Status.ToString(),
            CreatedAt = purchase.CreatedAt,
            UpdatedAt = purchase.UpdatedAt,
            Items = purchase.Items.Select(i => new PurchaseItemDto
            {
                Id = i.Id,
                InventoryItemId = i.InventoryItemId,
                InventoryItemName = i.InventoryItem?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                ExpirationDate = i.ExpirationDate
            }).ToList()
        };
    }
}
