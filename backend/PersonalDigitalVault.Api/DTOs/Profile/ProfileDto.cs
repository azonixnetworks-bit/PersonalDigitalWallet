namespace PersonalDigitalVault.Api.DTOs.Profile;

public class ProfileDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsTotpEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}
