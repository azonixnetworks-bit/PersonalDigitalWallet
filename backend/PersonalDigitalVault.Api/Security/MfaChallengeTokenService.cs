using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using PersonalDigitalVault.Api.Entities;

namespace PersonalDigitalVault.Api.Security;

public class MfaChallengeTokenService
{
    // =========================================================
    // CONFIGURATION
    // =========================================================

    private readonly IConfiguration _configuration;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Temporary login MFA token configuration access pannum.
    //
    // Input:
    // IConfiguration
    //
    // Reason:
    // Security:MfaTokenKey
    // Security:MfaIssuer
    // Security:MfaAudience
    //
    // values read panna.
    //
    // Output:
    // Configuration reference save aagum.
    public MfaChallengeTokenService(
        IConfiguration configuration)
    {
        _configuration =
            configuration;
    }


    // =========================================================
    // GENERATE LOGIN MFA CHALLENGE TOKEN
    // =========================================================

    // Function:
    // Password verification success piragu
    // temporary MFA challenge token generate pannum.
    //
    // Input:
    // Valid authenticated User object.
    //
    // Flow:
    //
    // Password correct
    //      ↓
    // Challenge Token
    //      ↓
    // TOTP verification
    //      ↓
    // FINAL JWT
    //
    // Output:
    // 5-minute temporary JWT.
    //
    // IMPORTANT:
    //
    // Idhu final application JWT illa.
    //
    // purpose = login_mfa
    //
    // Security:
    // Final Jwt:Key use panna maatom.
    // Security:MfaTokenKey use pannuvom.
    public string GenerateChallengeToken(
        User user)
    {
        // =====================================================
        // USER VALIDATION
        // =====================================================

        if (user == null)
        {
            throw new ArgumentNullException(
                nameof(user));
        }


        if (user.Id <= 0)
        {
            throw new InvalidOperationException(
                "Invalid user.");
        }


        // Disabled user-ku MFA challenge token
        // create panna koodathu.
        if (!user.IsActive)
        {
            throw new InvalidOperationException(
                "User account is not active.");
        }


        // Normal user login MFA-ku email verification
        // mandatory.
        if (!user.IsEmailVerified)
        {
            throw new InvalidOperationException(
                "Email verification is required.");
        }


        // TOTP setup complete aana user mattum
        // login MFA challenge receive panna mudiyum.
        if (!user.IsTotpEnabled ||
            string.IsNullOrWhiteSpace(
                user.TotpSecretEncrypted))
        {
            throw new InvalidOperationException(
                "Authenticator setup is required.");
        }


        // =====================================================
        // CONFIGURATION
        // =====================================================

        string? mfaTokenKey =
            _configuration[
                "Security:MfaTokenKey"
            ];


        string? issuer =
            _configuration[
                "Security:MfaIssuer"
            ];


        string? audience =
            _configuration[
                "Security:MfaAudience"
            ];


        // =====================================================
        // CONFIG VALIDATION
        // =====================================================

        if (string.IsNullOrWhiteSpace(
                mfaTokenKey))
        {
            throw new InvalidOperationException(
                "Security:MfaTokenKey is missing.");
        }


        if (string.IsNullOrWhiteSpace(
                issuer))
        {
            throw new InvalidOperationException(
                "Security:MfaIssuer is missing.");
        }


        if (string.IsNullOrWhiteSpace(
                audience))
        {
            throw new InvalidOperationException(
                "Security:MfaAudience is missing.");
        }


        // =====================================================
        // SIGNING KEY
        // =====================================================

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    mfaTokenKey));


        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);


        // =====================================================
        // TOKEN CLAIMS
        // =====================================================

        var claims =
            new List<Claim>
            {
                // Endha user password step complete panninaaro
                // avaroda database UserId.
                new Claim(
                    "userId",
                    user.Id.ToString()),


                // CRITICAL:
                //
                // Indha temporary token login MFA-ku mattum.
                //
                // Protected APIs-ku final access token-a
                // use panna mudiyathu.
                new Claim(
                    "purpose",
                    "login_mfa"),


                // User email.
                new Claim(
                    ClaimTypes.Email,
                    user.Email),


                // Ovvoru challenge token-kum
                // unique identifier.
                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid()
                        .ToString("N"))
            };


        // =====================================================
        // CREATE TOKEN
        // =====================================================

        var token =
            new JwtSecurityToken(
                issuer:
                    issuer,

                audience:
                    audience,

                claims:
                    claims,

                // Password verification mudinja piragu
                // exact 5 minutes mattum valid.
                expires:
                    DateTime.UtcNow
                        .AddMinutes(5),

                signingCredentials:
                    credentials);


        return new JwtSecurityTokenHandler()
            .WriteToken(
                token);
    }


    // =========================================================
    // VALIDATE LOGIN MFA CHALLENGE TOKEN
    // =========================================================

    // Function:
    // Password step-la return aana temporary
    // login_mfa JWT validate pannum.
    //
    // Input:
    // Challenge token.
    //
    // Security Checks:
    //
    // 1. Signature
    // 2. Issuer
    // 3. Audience
    // 4. Expiry
    // 5. Purpose == login_mfa
    // 6. Valid UserId
    //
    // Output:
    //
    // Valid:
    // UserId
    //
    // Invalid / expired:
    // null
    public int? ValidateChallengeToken(
        string token)
    {
        // =====================================================
        // EMPTY TOKEN
        // =====================================================

        if (string.IsNullOrWhiteSpace(
                token))
        {
            return null;
        }


        try
        {
            // =================================================
            // CONFIGURATION
            // =================================================

            string? mfaTokenKey =
                _configuration[
                    "Security:MfaTokenKey"
                ];


            string? issuer =
                _configuration[
                    "Security:MfaIssuer"
                ];


            string? audience =
                _configuration[
                    "Security:MfaAudience"
                ];


            if (string.IsNullOrWhiteSpace(
                    mfaTokenKey) ||
                string.IsNullOrWhiteSpace(
                    issuer) ||
                string.IsNullOrWhiteSpace(
                    audience))
            {
                return null;
            }


            // =================================================
            // SIGNING KEY
            // =================================================

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        mfaTokenKey));


            // =================================================
            // VALIDATION RULES
            // =================================================

            var parameters =
                new TokenValidationParameters
                {
                    // -----------------------------------------
                    // ISSUER
                    // -----------------------------------------

                    ValidateIssuer =
                        true,

                    ValidIssuer =
                        issuer,


                    // -----------------------------------------
                    // AUDIENCE
                    // -----------------------------------------

                    ValidateAudience =
                        true,

                    ValidAudience =
                        audience,


                    // -----------------------------------------
                    // SIGNATURE
                    // -----------------------------------------

                    ValidateIssuerSigningKey =
                        true,

                    IssuerSigningKey =
                        key,


                    // -----------------------------------------
                    // TOKEN EXPIRATION
                    // -----------------------------------------

                    ValidateLifetime =
                        true,


                    // Token must contain expiry.
                    RequireExpirationTime =
                        true,


                    // Token must be signed.
                    RequireSignedTokens =
                        true,


                    // =================================================
                    // R4 FIX
                    // =================================================
                    //
                    // OLD:
                    // ClockSkew = 30 seconds
                    //
                    // NEW:
                    // Challenge expires exactly at configured
                    // 5-minute expiry.
                    ClockSkew =
                        TimeSpan.Zero
                };


            // =================================================
            // VALIDATE TOKEN
            // =================================================

            var handler =
                new JwtSecurityTokenHandler();


            var principal =
                handler.ValidateToken(
                    token,
                    parameters,
                    out _);


            // =================================================
            // PURPOSE CHECK
            // =================================================

            string? purpose =
                principal
                    .FindFirst(
                        "purpose")?
                    .Value;


            // totp_setup token inga accepted aaga koodathu.
            if (!string.Equals(
                    purpose,
                    "login_mfa",
                    StringComparison.Ordinal))
            {
                return null;
            }


            // =================================================
            // USER ID
            // =================================================

            string? userIdText =
                principal
                    .FindFirst(
                        "userId")?
                    .Value;


            if (!int.TryParse(
                    userIdText,
                    out int userId))
            {
                return null;
            }


            if (userId <= 0)
            {
                return null;
            }


            // =================================================
            // SUCCESS
            // =================================================

            return userId;
        }
        catch
        {
            // Exact JWT failure external client-ku
            // expose panna maatom.
            //
            // Possible:
            // expired
            // modified
            // bad signature
            // wrong issuer
            // wrong audience
            // malformed JWT
            return null;
        }
    }
}