using System.ComponentModel.DataAnnotations;

namespace PersonalDigitalVault.Api.DTOs.Subscription;

public class VerifyCheckoutSessionRequestDto
{
    [Required]
    [StringLength(255, MinimumLength = 5)]
    public string SessionId { get; set; } = string.Empty;
}
