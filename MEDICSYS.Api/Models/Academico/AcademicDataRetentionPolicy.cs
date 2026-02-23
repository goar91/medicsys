namespace MEDICSYS.Api.Models.Academico;

public class AcademicDataRetentionPolicy
{
    public Guid Id { get; set; }
    public string DataCategory { get; set; } = string.Empty;
    public int RetentionMonths { get; set; }
    public bool AutoDelete { get; set; }
    public bool IsActive { get; set; }
    public Guid ConfiguredByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser ConfiguredByUser { get; set; } = null!;
}
