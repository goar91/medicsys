using Microsoft.AspNetCore.Identity;

namespace MEDICSYS.Api.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? UniversityId { get; set; }
}
