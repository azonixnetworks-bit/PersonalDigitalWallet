namespace PersonalDigitalVault.Api.DTOs.Auth;

public class TotpSetupResponseDto
{
    // Manual setup-ku secret
    // Setup time-la mattum frontend-ku show pannuvom
    public string SecretKey { get; set; }
        = string.Empty;


    // Authenticator URI
    public string OtpAuthUri { get; set; }
        = string.Empty;


    // QR image
    public string QrCodeDataUrl { get; set; }
        = string.Empty;
}