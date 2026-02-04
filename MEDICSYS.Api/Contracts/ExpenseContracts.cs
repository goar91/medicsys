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
    string Description,
    decimal Amount,
    DateTime ExpenseDate,
    string Category,
    string PaymentMethod,
    string? InvoiceNumber,
    string? Supplier,
    string? Notes
);

public record ExpenseUpdateRequest(
    string Description,
    decimal Amount,
    DateTime ExpenseDate,
    string Category,
    string PaymentMethod,
    string? InvoiceNumber,
    string? Supplier,
    string? Notes
);

public record ExpenseSummaryDto(
    decimal TotalExpenses,
    decimal MonthExpenses,
    decimal WeekExpenses,
    Dictionary<string, decimal> ExpensesByCategory,
    List<ExpenseDto> RecentExpenses
);
