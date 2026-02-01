using System.ComponentModel.DataAnnotations;

namespace MEDICSYS.Api.Contracts;

public class AuthLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
