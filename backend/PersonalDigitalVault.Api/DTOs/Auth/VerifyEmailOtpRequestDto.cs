namespace PersonalDigitalVault.Api.DTOs.Auth;

public class VerifyEmailOtpRequestDto
{
    // Register panna email
    public string Email { get; set; }
        = string.Empty;


    // Email-la receive panna
    // 6 digit OTP
    public string Otp { get; set; }
        = string.Empty;
}