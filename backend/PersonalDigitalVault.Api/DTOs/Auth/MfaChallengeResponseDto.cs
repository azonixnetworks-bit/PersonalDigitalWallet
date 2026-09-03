namespace PersonalDigitalVault.Api.DTOs.Auth;

public class MfaChallengeResponseDto
{
    // =====================================================
    // MFA
    // =====================================================

    // true-na frontend TOTP screen open pannum.
    public bool RequiresTotp { get; set; }


    // Client-ku password successful aana
    // temporary challenge token.
    public string? ChallengeToken { get; set; }


    // =====================================================
    // LOGIN DETAILS
    // =====================================================
    //
    // Admin direct login support panna
    // indha fields preserve pannrom.
    //

    public int? UserId { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Role { get; set; }


    // Final JWT.
    //
    // Client first login step-la NULL.
    // Admin direct login-la JWT irukkum.
    public string? Token { get; set; }


    public string Message { get; set; }
        = string.Empty;
}