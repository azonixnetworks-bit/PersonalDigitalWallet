namespace PersonalDigitalVault.Api.DTOs.Auth;

public class VerifyLoginTotpRequestDto
{
    // Password success appuram server
    // return panna 5-minute challenge token
    public string ChallengeToken { get; set; }
        = string.Empty;


    // Google Authenticator-la varra
    // current 6 digit code
    public string Code { get; set; }
        = string.Empty;
}