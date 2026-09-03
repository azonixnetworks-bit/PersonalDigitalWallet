namespace PersonalDigitalVault.Api.DTOs.Auth;

public class ResendEmailOtpRequestDto
{
    // OTP resend panna vendiya account email
    public string Email { get; set; }
        = string.Empty;
}