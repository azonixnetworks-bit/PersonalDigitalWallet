namespace PersonalDigitalVault.Api.DTOs.Auth;

public class SetupTotpRequestDto
{
    // Email verification success aana
    // server return panna temporary token
    public string SetupToken { get; set; }
        = string.Empty;
}