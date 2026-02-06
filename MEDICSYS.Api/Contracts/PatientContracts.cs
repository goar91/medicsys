using System.ComponentModel.DataAnnotations;

namespace MEDICSYS.Api.Contracts;

public class PatientDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public string? Allergies { get; set; }
    public string? Medications { get; set; }
    public string? Diseases { get; set; }
    public string? BloodType { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PatientCreateRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 6)]
    public string IdNumber { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [StringLength(20)]
    public string Gender { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(120)]
    public string? Email { get; set; }

    [StringLength(120)]
    public string? EmergencyContact { get; set; }

    [StringLength(20)]
    public string? EmergencyPhone { get; set; }

    [StringLength(500)]
    public string? Allergies { get; set; }

    [StringLength(500)]
    public string? Medications { get; set; }

    [StringLength(500)]
    public string? Diseases { get; set; }

    [StringLength(10)]
    public string? BloodType { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class PatientUpdateRequest
{
    [StringLength(100, MinimumLength = 2)]
    public string? FirstName { get; set; }

    [StringLength(100, MinimumLength = 2)]
    public string? LastName { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(120)]
    public string? Email { get; set; }

    [StringLength(120)]
    public string? EmergencyContact { get; set; }

    [StringLength(20)]
    public string? EmergencyPhone { get; set; }

    [StringLength(500)]
    public string? Allergies { get; set; }

    [StringLength(500)]
    public string? Medications { get; set; }

    [StringLength(500)]
    public string? Diseases { get; set; }

    [StringLength(10)]
    public string? BloodType { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
