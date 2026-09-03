using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using PersonalDigitalVault.Api.Entities;

namespace PersonalDigitalVault.Api.Security;

public class RegistrationTokenService
{
    // =========================================================
    // CONFIGURATION
    // =========================================================

    private readonly IConfiguration _configuration;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Temporary MFA token configuration access panna.
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
    // Configuration reference store aagum.
    public RegistrationTokenService(
        IConfiguration configuration)
    {
        _configuration =
            configuration;
    }


    // =========================================================
    // GENERATE TOTP SETUP TOKEN
    // =========================================================

    // Function:
    // Email verification success piragu
    // temporary Authenticator setup JWT create pannum.
    //
    // Input:
    // Verified active User.
    //
    // Output:
    // 10-minute temporary JWT.
    //
    // IMPORTANT:
    // Idhu FINAL access JWT illa.
    //
    // purpose = totp_setup
    //
    // Security:
    // Final JWT key use panna maatom.
    public string GenerateTotpSetupToken(
        User user)
    {
        // =====================================================
        // USER STATE VALIDATION
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


        // Disabled account-ku setup token create panna koodathu.
        if (!user.IsActive)
        {
            throw new InvalidOperationException(
                "User account is not active.");
        }


        // Email verification success aana user mattum
        // TOTP setup token receive panna mudiyum.
        if (!user.IsEmailVerified)
        {
            throw new InvalidOperationException(
                "Email verification is required.");
        }


        // Already TOTP enabled user-ku new setup token
        // create panna koodathu.
        if (user.IsTotpEnabled)
        {
            throw new InvalidOperationException(
                "Authenticator setup is already complete.");
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


        // Program.cs startup-la key length already validate
        // pannrom.
        //
        // Inga token create panna signing key prepare pannuvom.
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
                // Endha user-ku setup token issue pannom.
                new Claim(
                    "userId",
                    user.Id.ToString()),


                // Token purpose separation.
                //
                // login_mfa token inga accept aaga koodathu.
                new Claim(
                    "purpose",
                    "totp_setup"),


                // User email metadata.
                new Claim(
                    ClaimTypes.Email,
                    user.Email),


                // Ovvoru token-kum unique identifier.
                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid()
                        .ToString("N"))
            };


        // =====================================================
        // TOKEN CREATE
        // =====================================================

        var token =
            new JwtSecurityToken(
                issuer:
                    issuer,

                audience:
                    audience,

                claims:
                    claims,

                // Exact 10-minute lifetime.
                expires:
                    DateTime.UtcNow
                        .AddMinutes(10),

                signingCredentials:
                    credentials);


        return new JwtSecurityTokenHandler()
            .WriteToken(
                token);
    }


    // =========================================================
    // VALIDATE TOTP SETUP TOKEN
    // =========================================================

    // Function:
    // Temporary totp_setup JWT validate pannum.
    //
    // Input:
    // Setup token string.
    //
    // Checks:
    //
    // Signature
    // Issuer
    // Audience
    // Expiry
    // Purpose = totp_setup
    // UserId valid
    //
    // Output:
    //
    // Valid:
    // UserId
    //
    // Invalid:
    // null
    public int? ValidateTotpSetupToken(
        string token)
    {
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
                    // Issuer mandatory.
                    ValidateIssuer =
                        true,

                    ValidIssuer =
                        issuer,


                    // Audience mandatory.
                    ValidateAudience =
                        true,

                    ValidAudience =
                        audience,


                    // Signature mandatory.
                    ValidateIssuerSigningKey =
                        true,

                    IssuerSigningKey =
                        key,


                    // Expiry mandatory.
                    ValidateLifetime =
                        true,


                    // =================================================
                    // IMPORTANT R4 FIX
                    // =================================================
                    //
                    // OLD:
                    // ClockSkew = 30 seconds
                    //
                    // That could make a 10-minute token accepted
                    // slightly beyond intended expiry.
                    //
                    // NEW:
                    // Exact token expiry.
                    ClockSkew =
                        TimeSpan.Zero
                };


            // =================================================
            // VALIDATE JWT
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


            if (!string.Equals(
                    purpose,
                    "totp_setup",
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


            return userId;
        }
        catch
        {
            // Invalid JWT details external user-ku
            // expose panna maatom.
            //
            // Examples:
            // expired
            // modified
            // bad signature
            // wrong issuer
            // wrong audience
            return null;
        }
    }
}