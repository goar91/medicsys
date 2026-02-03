using System.ComponentModel.DataAnnotations;

namespace MEDICSYS.Api.Models;

public class AccountingCategory
{
    public Guid Id { get; set; }

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Group { get; set; } = string.Empty;

    public AccountingEntryType Type { get; set; }

    public decimal MonthlyBudget { get; set; }

    public bool IsActive { get; set; } = true;
}
