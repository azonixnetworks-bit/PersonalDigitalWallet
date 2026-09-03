namespace PersonalDigitalVault.Api.DTOs.Auth;

public class VerifyTotpSetupRequestDto
{
    // Temporary registration / setup token
    public string SetupToken { get; set; }
        = string.Empty;


    // Google Authenticator current
    // 6 digit code
    public string Code { get; set; }
        = string.Empty;
}