namespace MEDICSYS.Api.Models.Odontologia;

public class OdontologoPatient
{
    public Guid Id { get; set; }
    public Guid OdontologoId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string IdNumber { get; set; } = null!;
    public string DateOfBirth { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ApplicationUser Odontologo { get; set; } = null!;
}
