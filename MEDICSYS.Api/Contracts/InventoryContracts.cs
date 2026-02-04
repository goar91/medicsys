namespace MEDICSYS.Api.Contracts;

public record InventoryItemDto(
    Guid Id,
    string Name,
    string? Description,
    string? Sku,
    int Quantity,
    int MinimumQuantity,
    decimal UnitPrice,
    string? Supplier,
    DateTime? ExpirationDate,
    bool IsLowStock,
    bool IsExpiringSoon,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateInventoryItemRequest(
    string Name,
    string? Description,
    string? Sku,
    int Quantity,
    int MinimumQuantity,
    decimal UnitPrice,
    string? Supplier,
    DateTime? ExpirationDate
);

public record UpdateInventoryItemRequest(
    string Name,
    string? Description,
    string? Sku,
    int Quantity,
    int MinimumQuantity,
    decimal UnitPrice,
    string? Supplier,
    DateTime? ExpirationDate
);

public record InventoryAlertDto(
    Guid Id,
    Guid InventoryItemId,
    string Type,
    string Message,
    bool IsResolved,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    InventoryItemDto? InventoryItem
);
