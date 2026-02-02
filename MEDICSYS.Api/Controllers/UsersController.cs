using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [Authorize(Roles = Roles.Professor)]
    [HttpGet("students")]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetStudents()
    {
        var users = _userManager.Users.ToList();
        var result = new List<UserSummaryDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(Roles.Student))
            {
                result.Add(new UserSummaryDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty
                });
            }
        }
        return Ok(result);
    }

    [Authorize]
    [HttpGet("professors")]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetProfessors()
    {
        var users = _userManager.Users.ToList();
        var result = new List<UserSummaryDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(Roles.Professor))
            {
                result.Add(new UserSummaryDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty
                });
            }
        }
        return Ok(result);
    }
}

public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
