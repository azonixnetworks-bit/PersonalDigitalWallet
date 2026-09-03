using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PersonalDigitalVault.Api.DTOs.Auth;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;


[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // =========================================================
    // AUTH SERVICE
    // =========================================================
    //
    // Function:
    // Authentication business logic ellathayum
    // AuthService moolama handle panna use pannrom.
    //
    private readonly IAuthService _authService;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================
    //
    // Input:
    // IAuthService
    //
    // Reason:
    // Controller direct repository / security helper use pannaama
    // service layer moolama work panna.
    //
    // Output:
    // _authService variable-la service store aagum.
    //
    public AuthController(
        IAuthService authService)
    {
        _authService =
            authService;
    }


    // =========================================================
    // REGISTER
    // =========================================================
    //
    // API:
    // POST /api/auth/register
    //
    // Input:
    // FullName
    // Email
    // Password
    //
    // Security:
    // New email / existing email difference
    // public response-la expose panna maatom.
    //
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request)
    {
        string normalizedEmail =
            request.Email
                .Trim()
                .ToLowerInvariant();


        await _authService
            .RegisterAsync(
                request);


        // =====================================================
        // GENERIC RESPONSE
        // =====================================================
        //
        // Account already irukka illaya nu
        // attacker identify panna mudiyama same response.
        //
        return Ok(
            new
            {
                email =
                    normalizedEmail,

                requiresEmailVerification =
                    true,

                message =
                    "If registration is eligible, a verification code will be sent."
            });
    }


    // =========================================================
    // VERIFY EMAIL OTP
    // =========================================================
    //
    // API:
    // POST /api/auth/verify-email
    //
    // Input:
    // Email
    // OTP
    //
    // Success:
    // Email verified
    // +
    // temporary TOTP setup token.
    //
    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailOtpRequestDto request)
    {
        var result =
            await _authService
                .VerifyEmailOtpAsync(
                    request);


        if (result == null)
        {
            return BadRequest(
                new
                {
                    message =
                        "Verification could not be completed."
                });
        }


        return Ok(
            result);
    }


    // =========================================================
    // RESEND EMAIL OTP
    // =========================================================
    //
    // API:
    // POST /api/auth/resend-email-otp
    //
    // Security:
    // Account exists / verified / disabled / cooldown
    // details public response-la reveal panna maatom.
    //
    [AllowAnonymous]
    [HttpPost("resend-email-otp")]
    public async Task<IActionResult> ResendEmailOtp(
        [FromBody] ResendEmailOtpRequestDto request)
    {
        await _authService
            .ResendEmailOtpAsync(
                request);


        return Ok(
            new
            {
                message =
                    "If the account is eligible, a verification code will be sent."
            });
    }


    // =========================================================
    // FORGOT PASSWORD
    // =========================================================
    //
    // API:
    // POST /api/auth/forgot-password
    //
    // Input:
    //
    // {
    //     "email": "user@example.com"
    // }
    //
    // Flow:
    //
    // Email
    //   ↓
    // AuthService
    //   ↓
    // Eligible user check
    //   ↓
    // Secure reset token generate
    //   ↓
    // Token HASH database-la save
    //   ↓
    // Raw token email-la send
    //
    // SECURITY:
    //
    // Registered email
    // Unknown email
    // Disabled user
    // Admin user
    // Cooldown
    //
    // ellathukkum SAME public response.
    //
    // Reason:
    // Account enumeration prevent panna.
    //
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request)
    {
        // AuthService actual email send pannalama
        // silent-aa return pannalama nu decide pannum.
        //
        await _authService
            .ForgotPasswordAsync(
                request);


        // =====================================================
        // GENERIC PUBLIC RESPONSE
        // =====================================================
        //
        // Account registered-aa illaya nu
        // frontend-ku reveal panna koodathu.
        //
        return Ok(
            new
            {
                message =
                    "If the account is eligible, a password reset link will be sent."
            });
    }


    // =========================================================
    // RESET PASSWORD
    // =========================================================
    //
    // API:
    // POST /api/auth/reset-password
    //
    // Input:
    //
    // {
    //     "token": "...",
    //     "newPassword": "...",
    //     "confirmPassword": "..."
    // }
    //
    // Flow:
    //
    // Raw token
    //   ↓
    // SHA-256 hash
    //   ↓
    // DB token lookup
    //   ↓
    // Expiry check
    //   ↓
    // Single-use validation
    //   ↓
    // New password hash save
    //   ↓
    // Reset token clear
    //
    // IMPORTANT:
    // Password reset TOTP-a reset pannaathu.
    //
    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request)
    {
        bool success =
            await _authService
                .ResetPasswordAsync(
                    request);


        // =====================================================
        // INVALID / EXPIRED / USED TOKEN
        // =====================================================
        //
        // Exact reason frontend-ku expose panna maatom.
        //
        if (!success)
        {
            return BadRequest(
                new
                {
                    message =
                        "Password reset link is invalid or expired."
                });
        }


        // =====================================================
        // SUCCESS
        // =====================================================
        //
        // Automatic JWT/login inga kudukka maatom.
        //
        // User new password use panni
        // normal login + existing TOTP complete pannanum.
        //
        return Ok(
            new
            {
                message =
                    "Password reset successfully. Please sign in using your new password."
            });
    }


    // =========================================================
    // SETUP GOOGLE AUTHENTICATOR / TOTP
    // =========================================================
    //
    // API:
    // POST /api/auth/setup-totp
    //
    // Input:
    // SetupToken
    //
    // Output:
    // SecretKey
    // OtpAuthUri
    // QrCodeDataUrl
    //
    [AllowAnonymous]
    [HttpPost("setup-totp")]
    public async Task<IActionResult> SetupTotp(
        [FromBody] SetupTotpRequestDto request)
    {
        var result =
            await _authService
                .SetupTotpAsync(
                    request);


        if (result == null)
        {
            return BadRequest(
                new
                {
                    message =
                        "Authenticator setup could not be completed."
                });
        }


        return Ok(
            result);
    }


    // =========================================================
    // VERIFY GOOGLE AUTHENTICATOR SETUP
    // =========================================================
    //
    // API:
    // POST /api/auth/verify-totp-setup
    //
    // Input:
    // SetupToken
    // Authenticator 6-digit code
    //
    // Output:
    // Success / generic failure.
    //
    [AllowAnonymous]
    [HttpPost("verify-totp-setup")]
    public async Task<IActionResult> VerifyTotpSetup(
        [FromBody] VerifyTotpSetupRequestDto request)
    {
        bool success =
            await _authService
                .VerifyTotpSetupAsync(
                    request);


        if (!success)
        {
            return BadRequest(
                new
                {
                    message =
                        "Authenticator verification could not be completed."
                });
        }


        return Ok(
            new
            {
                message =
                    "Two-factor authentication enabled successfully."
            });
    }


    // =========================================================
    // LOGIN STEP 1
    // =========================================================
    //
    // API:
    // POST /api/auth/login
    //
    // Normal User:
    //
    // Email + Password
    //      ↓
    // Temporary MFA ChallengeToken
    //
    // Admin:
    //
    // Password
    //      ↓
    // Final JWT
    //
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request)
    {
        var result =
            await _authService
                .LoginAsync(
                    request);


        if (result == null)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Invalid login credentials or verification."
                });
        }


        return Ok(
            result);
    }


    // =========================================================
    // VERIFY LOGIN TOTP
    // =========================================================
    //
    // API:
    // POST /api/auth/verify-login-totp
    //
    // Input:
    // ChallengeToken
    // Authenticator Code
    //
    // Success:
    // Final application JWT.
    //
    [AllowAnonymous]
    [HttpPost("verify-login-totp")]
    public async Task<IActionResult> VerifyLoginTotp(
        [FromBody] VerifyLoginTotpRequestDto request)
    {
        var result =
            await _authService
                .VerifyLoginTotpAsync(
                    request);


        if (result == null)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Authenticator verification failed."
                });
        }


        return Ok(
            result);
    }


    // =========================================================
    // LOGOUT
    // =========================================================
    //
    // API:
    // POST /api/auth/logout
    //
    // Current system:
    // Stateless JWT.
    //
    // Frontend current JWT remove pannum.
    //
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return NoContent();
    }
}