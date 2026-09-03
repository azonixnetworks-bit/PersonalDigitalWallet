namespace PersonalDigitalVault.Api.DTOs.Auth;

public class EmailVerificationResponseDto
{
    // Email verified
    public bool EmailVerified { get; set; }


    // TOTP setup panna use pannura
    // temporary 10-minute token
    public string SetupToken { get; set; }
        = string.Empty;


    public string Message { get; set; }
        = string.Empty;
}