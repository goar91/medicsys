using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MEDICSYS.Api.Contracts;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Security;
using MEDICSYS.Api.Services;

namespace MEDICSYS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("register-student")]
    public async Task<ActionResult<AuthResponse>> RegisterStudent(AuthRegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
        {
            _logger.LogWarning("Intento de registro de alumno con email ya existente: {Email}", request.Email);
            return BadRequest("Email already registered.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            UniversityId = request.UniversityId
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Registro de alumno falló para {Email}: {Errors}", request.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        await _userManager.AddToRoleAsync(user, Roles.Student);

        var response = await BuildAuthResponseAsync(user);
        _logger.LogInformation("Alumno registrado {UserId} ({Email})", user.Id, user.Email);
        return Ok(response);
    }

    [Authorize(Roles = Roles.Professor)]
    [HttpPost("register-professor")]
    public async Task<ActionResult<AuthResponse>> RegisterProfessor(AuthRegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
        {
            _logger.LogWarning("Intento de registro de profesor con email ya existente: {Email}", request.Email);
            return BadRequest("Email already registered.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            UniversityId = request.UniversityId
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Registro de profesor falló para {Email}: {Errors}", request.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        await _userManager.AddToRoleAsync(user, Roles.Professor);

        var response = await BuildAuthResponseAsync(user);
        _logger.LogInformation("Profesor registrado {UserId} ({Email})", user.Id, user.Email);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(AuthLoginRequest request)
    {
        _logger.LogInformation("Intento de login para {Email}", request.Email);

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login rechazado para {Email}: usuario no encontrado", request.Email);
            return Unauthorized("Invalid credentials.");
        }

        var valid = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!valid.Succeeded)
        {
            _logger.LogWarning("Login rechazado para {Email}: contraseña inválida", request.Email);
            return Unauthorized("Invalid credentials.");
        }

        var response = await BuildAuthResponseAsync(user);
        _logger.LogInformation("Usuario {UserId} inició sesión como {Role}", user.Id, response.User.Role);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> Me()
    {
        var userId = GetUserId();
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? string.Empty,
            UniversityId = user.UniversityId
        });
    }

    private Guid GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new UnauthorizedAccessException();
        }

        return Guid.Parse(id);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = _tokenService.CreateToken(user, roles);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserProfileDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? string.Empty,
                UniversityId = user.UniversityId
            }
        };
    }
}
