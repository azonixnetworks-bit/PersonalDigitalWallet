namespace PersonalDigitalVault.Api.DTOs.Auth;

public class RegisterResponseDto
{
    // Newly created user ID
    public int UserId { get; set; }


    // OTP send panna email
    public string Email { get; set; }
        = string.Empty;


    // Frontend-ku next step sollum
    public bool RequiresEmailVerification { get; set; }
        = true;


    // User friendly message
    public string Message { get; set; }
        = string.Empty;
}