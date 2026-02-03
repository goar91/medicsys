namespace MEDICSYS.Api.Contracts;

public class AccountingCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal MonthlyBudget { get; set; }
    public bool IsActive { get; set; }
}

public class AccountingEntryRequest
{
    public DateTime Date { get; set; }
    public Guid CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public string? Reference { get; set; }
}

public class AccountingEntryDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryGroup { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public string Source { get; set; } = string.Empty;
    public Guid? InvoiceId { get; set; }
}

public class AccountingSummaryDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Net { get; set; }
    public List<AccountingGroupSummaryDto> Groups { get; set; } = new();
}

public class AccountingGroupSummaryDto
{
    public string Group { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Total { get; set; }
}
