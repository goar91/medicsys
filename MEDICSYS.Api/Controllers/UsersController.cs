using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Security;

namespace MEDICSYS.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [Authorize(Roles = Roles.Professor + "," + Roles.Odontologo + "," + Roles.Admin)]
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

    [Authorize]
    [HttpGet("odontologos")]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetOdontologos()
    {
        var users = _userManager.Users.ToList();
        var result = new List<UserSummaryDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(Roles.Odontologo))
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

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("admin/all")]
    public async Task<ActionResult<IEnumerable<UserAdminDto>>> GetAllForAdmin()
    {
        var users = _userManager.Users.OrderBy(u => u.FullName).ToList();
        var result = new List<UserAdminDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserAdminDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                UniversityId = user.UniversityId,
                Roles = roles.ToList()
            });
        }

        return Ok(result);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("admin")]
    public async Task<ActionResult<UserAdminDto>> CreateUserAsAdmin([FromBody] AdminCreateUserRequest request)
    {
        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();
        var role = request.Role.Trim();

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return BadRequest($"El rol '{role}' no existe.");
        }

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            return BadRequest("Ya existe un usuario con ese correo.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = fullName,
            UniversityId = request.UniversityId,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(createResult.Errors.Select(e => e.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            return BadRequest(roleResult.Errors.Select(e => e.Description));
        }

        return Ok(new UserAdminDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            UniversityId = user.UniversityId,
            Roles = new List<string> { role }
        });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("admin/{id:guid}")]
    public async Task<ActionResult<UserAdminDto>> UpdateUserAsAdmin(Guid id, [FromBody] AdminUpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalizedEmail = request.Email.Trim();
            var existing = await _userManager.FindByEmailAsync(normalizedEmail);
            if (existing != null && existing.Id != user.Id)
            {
                return BadRequest("Ya existe otro usuario con ese correo.");
            }

            user.Email = normalizedEmail;
            user.UserName = normalizedEmail;
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }

        user.UniversityId = request.UniversityId;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(updateResult.Errors.Select(e => e.Description));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var role = request.Role.Trim();
            if (!await _roleManager.RoleExistsAsync(role))
            {
                return BadRequest($"El rol '{role}' no existe.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return BadRequest(removeResult.Errors.Select(e => e.Description));
                }
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, role);
            if (!addRoleResult.Succeeded)
            {
                return BadRequest(addRoleResult.Errors.Select(e => e.Description));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
            if (!passwordResult.Succeeded)
            {
                return BadRequest(passwordResult.Errors.Select(e => e.Description));
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserAdminDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            UniversityId = user.UniversityId,
            Roles = roles.ToList()
        });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("admin/{id:guid}")]
    public async Task<IActionResult> DeleteUserAsAdmin(Guid id)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(actorId, out var actor) && actor == id)
        {
            return BadRequest("No puedes eliminar tu propio usuario administrador.");
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        return NoContent();
    }
}

public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UserAdminDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? UniversityId { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class AdminCreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? UniversityId { get; set; }
    public string Role { get; set; } = Roles.Student;
}

public class AdminUpdateUserRequest
{
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? UniversityId { get; set; }
    public string? Role { get; set; }
    public string? NewPassword { get; set; }
}
