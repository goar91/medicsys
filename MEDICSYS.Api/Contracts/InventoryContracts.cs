using System.ComponentModel.DataAnnotations;

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
    [param: Required]
    [param: StringLength(150, MinimumLength = 2)]
    string Name,
    [param: StringLength(300)]
    string? Description,
    [param: StringLength(60)]
    string? Sku,
    [param: Range(0, 100_000)]
    int Quantity,
    [param: Range(0, 100_000)]
    int MinimumQuantity,
    [param: Range(0, 1_000_000)]
    decimal UnitPrice,
    [param: StringLength(120)]
    string? Supplier,
    DateTime? ExpirationDate
);

public record UpdateInventoryItemRequest(
    [param: Required]
    [param: StringLength(150, MinimumLength = 2)]
    string Name,
    [param: StringLength(300)]
    string? Description,
    [param: StringLength(60)]
    string? Sku,
    [param: Range(0, 100_000)]
    int Quantity,
    [param: Range(0, 100_000)]
    int MinimumQuantity,
    [param: Range(0, 1_000_000)]
    decimal UnitPrice,
    [param: StringLength(120)]
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
