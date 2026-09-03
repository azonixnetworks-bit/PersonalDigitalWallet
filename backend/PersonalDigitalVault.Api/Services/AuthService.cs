using PersonalDigitalVault.Api.DTOs.Auth;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;

namespace PersonalDigitalVault.Api.Services;

public class AuthService : IAuthService
{
    // =========================================================
    // DEPENDENCIES
    // =========================================================

    private readonly IUserRepository
        _userRepository;

    private readonly PasswordHasher
        _passwordHasher;

    private readonly JwtTokenGenerator
        _jwtTokenGenerator;

    private readonly EmailOtpService
        _emailOtpService;

    private readonly IEmailService
        _emailService;

    private readonly RegistrationTokenService
        _registrationTokenService;

    private readonly TotpService
        _totpService;

    private readonly TotpSecretProtector
        _totpSecretProtector;

    private readonly MfaChallengeTokenService
        _mfaChallengeTokenService;

    // Password reset token:
    // Generate
    // Hash
    // Verify
    private readonly PasswordResetTokenService
        _passwordResetTokenService;

    // Sensitive token values log pannaama
    // operational errors mattum log panna.
    private readonly ILogger<AuthService>
        _logger;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public AuthService(
        IUserRepository userRepository,
        PasswordHasher passwordHasher,
        JwtTokenGenerator jwtTokenGenerator,
        EmailOtpService emailOtpService,
        IEmailService emailService,
        RegistrationTokenService registrationTokenService,
        TotpService totpService,
        TotpSecretProtector totpSecretProtector,
        MfaChallengeTokenService mfaChallengeTokenService,
        PasswordResetTokenService passwordResetTokenService,
        ILogger<AuthService> logger)
    {
        _userRepository =
            userRepository;

        _passwordHasher =
            passwordHasher;

        _jwtTokenGenerator =
            jwtTokenGenerator;

        _emailOtpService =
            emailOtpService;

        _emailService =
            emailService;

        _registrationTokenService =
            registrationTokenService;

        _totpService =
            totpService;

        _totpSecretProtector =
            totpSecretProtector;

        _mfaChallengeTokenService =
            mfaChallengeTokenService;

        _passwordResetTokenService =
            passwordResetTokenService;

        _logger =
            logger;
    }


    // =========================================================
    // REGISTER USER
    // =========================================================

    public async Task<RegisterResponseDto?>
        RegisterAsync(
            RegisterRequestDto request)
    {
        string email =
            request.Email
                .Trim()
                .ToLowerInvariant();


        var existingUser =
            await _userRepository
                .GetByEmailAsync(
                    email);


        if (existingUser != null)
        {
            return null;
        }


        // Email verification OTP generate.
        string otp =
            _emailOtpService
                .GenerateOtp();


        string otpHash =
            _emailOtpService
                .HashOtp(
                    otp);


        var user =
            new User
            {
                FullName =
                    request.FullName.Trim(),

                Email =
                    email,

                Role =
                    "User",

                IsActive =
                    true,

                CreatedAt =
                    DateTime.UtcNow,

                IsEmailVerified =
                    false,

                EmailOtpHash =
                    otpHash,

                EmailOtpExpiresAt =
                    DateTime.UtcNow
                        .AddMinutes(5),

                EmailOtpAttempts =
                    0,

                EmailOtpLastSentAt =
                    DateTime.UtcNow,

                IsTotpEnabled =
                    false,

                TotpSecretEncrypted =
                    null,

                LastTotpTimeStepUsed =
                    null,

                // New account-ku password reset
                // request initially illa.
                PasswordResetTokenHash =
                    null,

                PasswordResetTokenExpiresAt =
                    null,

                PasswordResetLastSentAt =
                    null
            };


        // Plain password DB-la save panna maatom.
        user.PasswordHash =
            _passwordHasher
                .HashPassword(
                    user,
                    request.Password);


        await _userRepository
            .AddAsync(
                user);


        await _emailService
            .SendVerificationOtpAsync(
                user.Email,
                user.FullName,
                otp);


        return new RegisterResponseDto
        {
            UserId =
                user.Id,

            Email =
                user.Email,

            RequiresEmailVerification =
                true,

            Message =
                "Verification code sent to your email."
        };
    }


    // =========================================================
    // VERIFY EMAIL OTP
    // =========================================================

    public async Task<EmailVerificationResponseDto?>
        VerifyEmailOtpAsync(
            VerifyEmailOtpRequestDto request)
    {
        string email =
            request.Email
                .Trim()
                .ToLowerInvariant();


        var user =
            await _userRepository
                .GetByEmailAsync(
                    email);


        if (user == null)
        {
            return null;
        }


        if (!user.IsActive)
        {
            return null;
        }


        if (user.IsEmailVerified)
        {
            return null;
        }


        if (string.IsNullOrWhiteSpace(
                user.EmailOtpHash)
            ||
            user.EmailOtpExpiresAt == null)
        {
            return null;
        }


        // Maximum 5 wrong attempts.
        if (user.EmailOtpAttempts >= 5)
        {
            return null;
        }


        if (DateTime.UtcNow >
            user.EmailOtpExpiresAt.Value)
        {
            return null;
        }


        bool isCorrect =
            _emailOtpService
                .VerifyOtp(
                    request.Otp,
                    user.EmailOtpHash);


        if (!isCorrect)
        {
            user.EmailOtpAttempts++;


            await _userRepository
                .UpdateAsync(
                    user);


            return null;
        }


        // Email verification success.
        user.IsEmailVerified =
            true;


        // Used OTP remove.
        user.EmailOtpHash =
            null;


        user.EmailOtpExpiresAt =
            null;


        user.EmailOtpAttempts =
            0;


        await _userRepository
            .UpdateAsync(
                user);


        // Final JWT illa.
        // Temporary TOTP setup token mattum.
        string setupToken =
            _registrationTokenService
                .GenerateTotpSetupToken(
                    user);


        return new EmailVerificationResponseDto
        {
            EmailVerified =
                true,

            SetupToken =
                setupToken,

            Message =
                "Email verified successfully. Continue authenticator setup."
        };
    }


    // =========================================================
    // RESEND EMAIL OTP
    // =========================================================

    public async Task<bool>
        ResendEmailOtpAsync(
            ResendEmailOtpRequestDto request)
    {
        string email =
            request.Email
                .Trim()
                .ToLowerInvariant();


        var user =
            await _userRepository
                .GetByEmailAsync(
                    email);


        if (user == null)
        {
            return false;
        }


        if (!user.IsActive)
        {
            return false;
        }


        if (user.IsEmailVerified)
        {
            return false;
        }


        // 60-second resend cooldown.
        if (user.EmailOtpLastSentAt.HasValue)
        {
            DateTime nextAllowed =
                user.EmailOtpLastSentAt
                    .Value
                    .AddSeconds(60);


            if (DateTime.UtcNow <
                nextAllowed)
            {
                return false;
            }
        }


        string otp =
            _emailOtpService
                .GenerateOtp();


        user.EmailOtpHash =
            _emailOtpService
                .HashOtp(
                    otp);


        user.EmailOtpExpiresAt =
            DateTime.UtcNow
                .AddMinutes(5);


        user.EmailOtpAttempts =
            0;


        user.EmailOtpLastSentAt =
            DateTime.UtcNow;


        await _userRepository
            .UpdateAsync(
                user);


        await _emailService
            .SendVerificationOtpAsync(
                user.Email,
                user.FullName,
                otp);


        return true;
    }


    // =========================================================
    // FORGOT PASSWORD
    // =========================================================
    //
    // Function:
    // Eligible normal user-ku secure password
    // reset email send pannum.
    //
    // Security:
    // Controller same generic response return pannum.
    //
    public async Task ForgotPasswordAsync(
        ForgotPasswordRequestDto request)
    {
        string email =
            request.Email
                .Trim()
                .ToLowerInvariant();


        var user =
            await _userRepository
                .GetByEmailAsync(
                    email);


        // Unknown account.
        // Account existence reveal panna maatom.
        if (user == null)
        {
            return;
        }


        // Disabled account.
        if (!user.IsActive)
        {
            return;
        }


        // Public forgot-password flow
        // normal User role-ku mattum.
        bool isNormalUser =
            string.Equals(
                user.Role,
                "User",
                StringComparison.OrdinalIgnoreCase);


        if (!isNormalUser)
        {
            return;
        }


        // Email ownership verify aana
        // account mattum recovery use pannalaam.
        if (!user.IsEmailVerified)
        {
            return;
        }


        // =====================================================
        // 60 SECOND COOLDOWN
        // =====================================================

        if (user.PasswordResetLastSentAt.HasValue)
        {
            DateTime nextAllowedTime =
                user.PasswordResetLastSentAt
                    .Value
                    .AddSeconds(60);


            if (DateTime.UtcNow <
                nextAllowedTime)
            {
                return;
            }
        }


        // =====================================================
        // GENERATE RAW TOKEN
        // =====================================================

        string rawToken =
            _passwordResetTokenService
                .GenerateToken();


        // =====================================================
        // HASH TOKEN
        // =====================================================
        //
        // DB-la raw token save panna maatom.
        //
        string tokenHash =
            _passwordResetTokenService
                .HashToken(
                    rawToken);


        // =====================================================
        // SAVE RESET STATE
        // =====================================================

        user.PasswordResetTokenHash =
            tokenHash;


        // 15 minutes only.
        user.PasswordResetTokenExpiresAt =
            DateTime.UtcNow
                .AddMinutes(15);


        user.PasswordResetLastSentAt =
            DateTime.UtcNow;


        await _userRepository
            .UpdateAsync(
                user);


        // =====================================================
        // SEND EMAIL
        // =====================================================

        try
        {
            await _emailService
                .SendPasswordResetEmailAsync(
                    user.Email,
                    user.FullName,
                    rawToken);
        }
        catch (Exception exception)
        {
            // Email send fail aana
            // unusable token invalidate pannuvom.
            user.PasswordResetTokenHash =
                null;


            user.PasswordResetTokenExpiresAt =
                null;


            await _userRepository
                .UpdateAsync(
                    user);


            // Raw token / SMTP password log panna maatom.
            _logger.LogWarning(
                exception,
                "Password reset email could not be sent for user {UserId}.",
                user.Id);
        }
    }


    // =========================================================
    // RESET PASSWORD
    // =========================================================

    public async Task<bool> ResetPasswordAsync(
        ResetPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(
                request.Token))
        {
            return false;
        }


        // Browser raw token-a same SHA-256
        // algorithm use panni hash pannuvom.
        string tokenHash;


        try
        {
            tokenHash =
                _passwordResetTokenService
                    .HashToken(
                        request.Token.Trim());
        }
        catch
        {
            return false;
        }


        var user =
            await _userRepository
                .GetByPasswordResetTokenHashAsync(
                    tokenHash);


        if (user == null)
        {
            return false;
        }


        if (!user.IsActive)
        {
            return false;
        }


        bool isNormalUser =
            string.Equals(
                user.Role,
                "User",
                StringComparison.OrdinalIgnoreCase);


        if (!isNormalUser)
        {
            return false;
        }


        if (!user.IsEmailVerified)
        {
            return false;
        }


        if (string.IsNullOrWhiteSpace(
                user.PasswordResetTokenHash)
            ||
            !user.PasswordResetTokenExpiresAt.HasValue)
        {
            return false;
        }


        // =====================================================
        // EXPIRY
        // =====================================================

        if (DateTime.UtcNow >
            user.PasswordResetTokenExpiresAt.Value)
        {
            user.PasswordResetTokenHash =
                null;


            user.PasswordResetTokenExpiresAt =
                null;


            await _userRepository
                .UpdateAsync(
                    user);


            return false;
        }


        // =====================================================
        // FIXED-TIME TOKEN VERIFY
        // =====================================================

        bool tokenValid =
            _passwordResetTokenService
                .VerifyToken(
                    request.Token.Trim(),
                    user.PasswordResetTokenHash);


        if (!tokenValid)
        {
            return false;
        }


        // =====================================================
        // NEW PASSWORD HASH
        // =====================================================

        user.PasswordHash =
            _passwordHasher
                .HashPassword(
                    user,
                    request.NewPassword);


        // =====================================================
        // SINGLE USE TOKEN
        // =====================================================

        user.PasswordResetTokenHash =
            null;


        user.PasswordResetTokenExpiresAt =
            null;


        user.PasswordResetLastSentAt =
            null;


        // IMPORTANT:
        // Existing MFA settings untouched.
        //
        // IsTotpEnabled
        // TotpSecretEncrypted
        // LastTotpTimeStepUsed
        //
        // ellame preserve aagum.


        await _userRepository
            .UpdateAsync(
                user);


        return true;
    }


    // =========================================================
    // SETUP TOTP
    // =========================================================

    public async Task<TotpSetupResponseDto?>
        SetupTotpAsync(
            SetupTotpRequestDto request)
    {
        int? userId =
            _registrationTokenService
                .ValidateTotpSetupToken(
                    request.SetupToken);


        if (userId == null)
        {
            return null;
        }


        var user =
            await _userRepository
                .GetByIdAsync(
                    userId.Value);


        if (user == null)
        {
            return null;
        }


        if (!user.IsActive)
        {
            return null;
        }


        if (!user.IsEmailVerified)
        {
            return null;
        }


        // Authenticator setup already complete.
        if (user.IsTotpEnabled)
        {
            return null;
        }


        string secret;


        // Pending secret already irundha
        // same secret reuse pannuvom.
        if (!string.IsNullOrWhiteSpace(
                user.TotpSecretEncrypted))
        {
            try
            {
                secret =
                    _totpSecretProtector
                        .Decrypt(
                            user.TotpSecretEncrypted);
            }
            catch
            {
                return null;
            }
        }
        else
        {
            secret =
                _totpService
                    .GenerateSecret();


            string encryptedSecret =
                _totpSecretProtector
                    .Encrypt(
                        secret);


            user.TotpSecretEncrypted =
                encryptedSecret;


            user.IsTotpEnabled =
                false;


            await _userRepository
                .UpdateAsync(
                    user);
        }


        string otpAuthUri =
            _totpService
                .GenerateOtpAuthUri(
                    secret,
                    user.Email);


        string qrCode =
            _totpService
                .GenerateQrCodeDataUrl(
                    otpAuthUri);


        return new TotpSetupResponseDto
        {
            SecretKey =
                secret,

            OtpAuthUri =
                otpAuthUri,

            QrCodeDataUrl =
                qrCode
        };
    }


    // =========================================================
    // VERIFY INITIAL TOTP SETUP
    // =========================================================

    public async Task<bool>
        VerifyTotpSetupAsync(
            VerifyTotpSetupRequestDto request)
    {
        int? userId =
            _registrationTokenService
                .ValidateTotpSetupToken(
                    request.SetupToken);


        if (userId == null)
        {
            return false;
        }


        var user =
            await _userRepository
                .GetByIdAsync(
                    userId.Value);


        if (user == null)
        {
            return false;
        }


        if (!user.IsActive)
        {
            return false;
        }


        if (!user.IsEmailVerified)
        {
            return false;
        }


        // Retry/idempotent behavior preserve.
        if (user.IsTotpEnabled)
        {
            return true;
        }


        if (string.IsNullOrWhiteSpace(
                user.TotpSecretEncrypted))
        {
            return false;
        }


        string secret;


        try
        {
            secret =
                _totpSecretProtector
                    .Decrypt(
                        user.TotpSecretEncrypted);
        }
        catch
        {
            return false;
        }


        bool valid =
            _totpService
                .VerifyCode(
                    secret,
                    request.Code);


        if (!valid)
        {
            return false;
        }


        user.IsTotpEnabled =
            true;


        // Login replay history fresh-aa
        // start aagattum.
        user.LastTotpTimeStepUsed =
            null;


        await _userRepository
            .UpdateAsync(
                user);


        return true;
    }


    // =========================================================
    // LOGIN STEP 1 — PASSWORD
    // =========================================================

    public async Task<MfaChallengeResponseDto?>
        LoginAsync(
            LoginRequestDto request)
    {
        string email =
            request.Email
                .Trim()
                .ToLowerInvariant();


        var user =
            await _userRepository
                .GetByEmailAsync(
                    email);


        if (user == null)
        {
            return null;
        }


        if (!user.IsActive)
        {
            return null;
        }


        bool passwordCorrect =
            _passwordHasher
                .VerifyPassword(
                    user,
                    user.PasswordHash,
                    request.Password);


        if (!passwordCorrect)
        {
            return null;
        }


        bool isAdmin =
            string.Equals(
                user.Role,
                "Admin",
                StringComparison.OrdinalIgnoreCase);


        // =====================================================
        // ADMIN LOGIN
        // =====================================================
        //
        // Existing frozen behavior preserve.
        //
        // Admin:
        // Password
        //    ↓
        // Final JWT
        //
        // No Email OTP / TOTP requirement.
        //
        if (isAdmin)
        {
            string adminToken =
                _jwtTokenGenerator
                    .GenerateToken(
                        user);


            return new MfaChallengeResponseDto
            {
                RequiresTotp =
                    false,

                ChallengeToken =
                    null,

                UserId =
                    user.Id,

                FullName =
                    user.FullName,

                Email =
                    user.Email,

                Role =
                    user.Role,

                Token =
                    adminToken,

                Message =
                    "Admin login successful."
            };
        }


        // Normal User only after this point.
        if (!user.IsEmailVerified)
        {
            return null;
        }


        if (!user.IsTotpEnabled
            ||
            string.IsNullOrWhiteSpace(
                user.TotpSecretEncrypted))
        {
            return null;
        }


        string challengeToken =
            _mfaChallengeTokenService
                .GenerateChallengeToken(
                    user);


        return new MfaChallengeResponseDto
        {
            RequiresTotp =
                true,

            ChallengeToken =
                challengeToken,

            UserId =
                user.Id,

            FullName =
                user.FullName,

            Email =
                user.Email,

            Role =
                user.Role,

            Token =
                null,

            Message =
                "Password verified. Enter your authenticator code."
        };
    }


    // =========================================================
    // LOGIN STEP 2 — VERIFY TOTP
    // =========================================================

    public async Task<LoginResponseDto?>
        VerifyLoginTotpAsync(
            VerifyLoginTotpRequestDto request)
    {
        int? userId =
            _mfaChallengeTokenService
                .ValidateChallengeToken(
                    request.ChallengeToken);


        if (userId == null)
        {
            return null;
        }


        var user =
            await _userRepository
                .GetByIdAsync(
                    userId.Value);


        if (user == null)
        {
            return null;
        }


        if (!user.IsActive)
        {
            return null;
        }


        if (!user.IsEmailVerified)
        {
            return null;
        }


        if (!user.IsTotpEnabled
            ||
            string.IsNullOrWhiteSpace(
                user.TotpSecretEncrypted))
        {
            return null;
        }


        string secret;


        try
        {
            secret =
                _totpSecretProtector
                    .Decrypt(
                        user.TotpSecretEncrypted);
        }
        catch
        {
            return null;
        }


        bool valid =
            _totpService
                .VerifyCode(
                    secret,
                    request.Code,
                    out long matchedTimeStep);


        if (!valid)
        {
            return null;
        }


        // =====================================================
        // TOTP REPLAY PROTECTION
        // =====================================================

        if (user.LastTotpTimeStepUsed.HasValue
            &&
            matchedTimeStep <=
            user.LastTotpTimeStepUsed.Value)
        {
            return null;
        }


        // JWT issue panna munnadi
        // accepted time-step DB-la save pannuvom.
        user.LastTotpTimeStepUsed =
            matchedTimeStep;


        await _userRepository
            .UpdateAsync(
                user);


        string token =
            _jwtTokenGenerator
                .GenerateToken(
                    user);


        return new LoginResponseDto
        {
            UserId =
                user.Id,

            FullName =
                user.FullName,

            Email =
                user.Email,

            Role =
                user.Role,

            Token =
                token
        };
    }
}