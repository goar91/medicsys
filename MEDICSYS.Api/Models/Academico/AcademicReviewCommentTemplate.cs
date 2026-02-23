namespace MEDICSYS.Api.Models.Academico;

public class AcademicReviewCommentTemplate
{
    public Guid Id { get; set; }
    public Guid ProfessorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CommentText { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ApplicationUser Professor { get; set; } = null!;
}
