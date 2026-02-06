using System.ComponentModel.DataAnnotations;

namespace MEDICSYS.Api.Contracts;

public record ExpenseDto(
    Guid Id,
    Guid OdontologoId,
    string Description,
    decimal Amount,
    DateTime ExpenseDate,
    string Category,
    string PaymentMethod,
    string? InvoiceNumber,
    string? Supplier,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record ExpenseCreateRequest(
    [param: Required]
    [param: StringLength(200, MinimumLength = 3)]
    string Description,
    [param: Range(0.01, 1_000_000)]
    decimal Amount,
    [param: Required]
    DateTime ExpenseDate,
    [param: Required]
    [param: StringLength(100)]
    string Category,
    [param: Required]
    [param: StringLength(50)]
    string PaymentMethod,
    [param: StringLength(120)]
    string? InvoiceNumber,
    [param: StringLength(120)]
    string? Supplier,
    [param: StringLength(500)]
    string? Notes
);

public record ExpenseUpdateRequest(
    [param: Required]
    [param: StringLength(200, MinimumLength = 3)]
    string Description,
    [param: Range(0.01, 1_000_000)]
    decimal Amount,
    [param: Required]
    DateTime ExpenseDate,
    [param: Required]
    [param: StringLength(100)]
    string Category,
    [param: Required]
    [param: StringLength(50)]
    string PaymentMethod,
    [param: StringLength(120)]
    string? InvoiceNumber,
    [param: StringLength(120)]
    string? Supplier,
    [param: StringLength(500)]
    string? Notes
);

public record ExpenseSummaryDto(
    decimal TotalExpenses,
    decimal MonthExpenses,
    decimal WeekExpenses,
    Dictionary<string, decimal> ExpensesByCategory,
    List<ExpenseDto> RecentExpenses
);
