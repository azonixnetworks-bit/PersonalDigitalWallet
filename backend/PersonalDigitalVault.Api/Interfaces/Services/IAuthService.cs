using PersonalDigitalVault.Api.DTOs.Auth;

namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface IAuthService
{
    // =====================================================
    // REGISTER
    // =====================================================
    //
    // Function:
    // New normal user register pannum.
    //
    // Output:
    // RegisterResponseDto or null.
    //
    Task<RegisterResponseDto?> RegisterAsync(
        RegisterRequestDto request);


    // =====================================================
    // VERIFY EMAIL OTP
    // =====================================================
    //
    // Function:
    // Registration email OTP verify pannum.
    //
    // Output:
    // Email verification result + TOTP setup token.
    //
    Task<EmailVerificationResponseDto?>
        VerifyEmailOtpAsync(
            VerifyEmailOtpRequestDto request);


    // =====================================================
    // RESEND EMAIL OTP
    // =====================================================
    //
    // Function:
    // Eligible unverified user-ku new email OTP send pannum.
    //
    Task<bool> ResendEmailOtpAsync(
        ResendEmailOtpRequestDto request);


    // =====================================================
    // FORGOT PASSWORD
    // =====================================================
    //
    // Function:
    // Password marantha normal user-ku secure
    // password reset email process pannum.
    //
    // Security:
    // Account exist / not exist public response-la
    // reveal panna koodathu.
    //
    Task ForgotPasswordAsync(
        ForgotPasswordRequestDto request);


    // =====================================================
    // RESET PASSWORD
    // =====================================================
    //
    // Function:
    // Secure reset token verify panni
    // new password save pannum.
    //
    // Output:
    // true  = reset success
    // false = invalid / expired / used token
    //
    Task<bool> ResetPasswordAsync(
        ResetPasswordRequestDto request);


    // =====================================================
    // SETUP TOTP
    // =====================================================
    //
    // Function:
    // Google Authenticator setup QR generate pannum.
    //
    Task<TotpSetupResponseDto?>
        SetupTotpAsync(
            SetupTotpRequestDto request);


    // =====================================================
    // VERIFY INITIAL TOTP
    // =====================================================
    //
    // Function:
    // Authenticator setup code verify pannum.
    //
    Task<bool> VerifyTotpSetupAsync(
        VerifyTotpSetupRequestDto request);


    // =====================================================
    // LOGIN STEP 1
    // =====================================================
    //
    // Admin:
    // Password correct
    //      ↓
    // Final JWT
    //
    // Normal User:
    // Password correct
    //      ↓
    // Temporary MFA challenge token
    //
    Task<MfaChallengeResponseDto?>
        LoginAsync(
            LoginRequestDto request);


    // =====================================================
    // LOGIN STEP 2
    // =====================================================
    //
    // Normal User:
    // MFA challenge + TOTP
    //      ↓
    // Final JWT
    //
    Task<LoginResponseDto?>
        VerifyLoginTotpAsync(
            VerifyLoginTotpRequestDto request);
}