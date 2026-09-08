using System.ComponentModel.DataAnnotations;

namespace PersonalDigitalVault.Api.DTOs.Profile;

public class UpdateProfileDto
{
    [Required]
    [StringLength(200)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Full name is required.")]
    public string FullName { get; set; } = string.Empty;
}
