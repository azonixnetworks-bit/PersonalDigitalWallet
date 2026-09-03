using Microsoft.IdentityModel.Tokens;

using PersonalDigitalVault.Api.Entities;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PersonalDigitalVault.Api.Security;

public class JwtTokenGenerator
{
    // =========================================================
    // CONFIGURATION
    // =========================================================

    private readonly IConfiguration _configuration;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // JWT configuration values access panna.
    //
    // Input:
    // IConfiguration
    //
    // Reason:
    // Jwt:Key
    // Jwt:Issuer
    // Jwt:Audience
    // Jwt:ExpiryMinutes
    //
    // values read panna.
    //
    // Output:
    // Configuration reference store aagum.
    public JwtTokenGenerator(
        IConfiguration configuration)
    {
        _configuration =
            configuration;
    }


    // =========================================================
    // GENERATE FINAL ACCESS TOKEN
    // =========================================================

    // Function:
    // Full authentication complete aana user-ku
    // FINAL application JWT generate pannum.
    //
    // Input:
    // User entity.
    //
    // Output:
    // JWT token string.
    //
    // IMPORTANT:
    //
    // Idhu:
    // purpose = access
    //
    // token.
    //
    // Temporary:
    //
    // login_mfa
    // totp_setup
    //
    // tokens inga generate panna maatom.
    public string GenerateToken(
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


        // Admin disable pannina user-ku
        // new access token issue panna koodathu.
        if (!user.IsActive)
        {
            throw new InvalidOperationException(
                "User account is not active.");
        }


        if (string.IsNullOrWhiteSpace(
                user.Role))
        {
            throw new InvalidOperationException(
                "User role is missing.");
        }


        // =====================================================
        // ROLE-SPECIFIC SECURITY
        // =====================================================

        bool isAdmin =
            string.Equals(
                user.Role,
                "Admin",
                StringComparison.OrdinalIgnoreCase);


        bool isNormalUser =
            string.Equals(
                user.Role,
                "User",
                StringComparison.OrdinalIgnoreCase);


        // Current PDV roles:
        // User / Admin
        //
        // Unknown role accidentally token receive panna koodathu.
        if (!isAdmin &&
            !isNormalUser)
        {
            throw new InvalidOperationException(
                "Invalid user role.");
        }


        // Normal vault user-ku final JWT issue panna:
        //
        // Email verified
        // +
        // TOTP setup complete
        //
        // mandatory.
        //
        // Admin current agreed login flow
        // password-only behavior preserve pannrom.
        if (isNormalUser)
        {
            if (!user.IsEmailVerified)
            {
                throw new InvalidOperationException(
                    "Email verification is required.");
            }


            if (!user.IsTotpEnabled ||
                string.IsNullOrWhiteSpace(
                    user.TotpSecretEncrypted))
            {
                throw new InvalidOperationException(
                    "Authenticator verification is required.");
            }
        }


        // =====================================================
        // JWT CONFIGURATION
        // =====================================================

        string? key =
            _configuration[
                "Jwt:Key"
            ];


        string? issuer =
            _configuration[
                "Jwt:Issuer"
            ];


        string? audience =
            _configuration[
                "Jwt:Audience"
            ];


        if (string.IsNullOrWhiteSpace(
                key))
        {
            throw new InvalidOperationException(
                "Jwt:Key is missing.");
        }


        if (string.IsNullOrWhiteSpace(
                issuer))
        {
            throw new InvalidOperationException(
                "Jwt:Issuer is missing.");
        }


        if (string.IsNullOrWhiteSpace(
                audience))
        {
            throw new InvalidOperationException(
                "Jwt:Audience is missing.");
        }


        // Defense-in-depth:
        // Program.cs already minimum key length check pannum.
        if (Encoding.UTF8
                .GetByteCount(
                    key) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key must contain at least 32 bytes.");
        }


        // =====================================================
        // TOKEN EXPIRY
        // =====================================================

        // Existing project default:
        // 120 minutes.
        //
        // Jwt:ExpiryMinutes configure pannirundha
        // adha use pannuvom.
        int expiryMinutes =
            120;


        string? expiryValue =
            _configuration[
                "Jwt:ExpiryMinutes"
            ];


        if (!string.IsNullOrWhiteSpace(
                expiryValue))
        {
            if (!int.TryParse(
                    expiryValue,
                    out expiryMinutes))
            {
                throw new InvalidOperationException(
                    "Jwt:ExpiryMinutes is invalid.");
            }
        }


        // Accidentally zero / negative / extremely long
        // access token configure panna koodathu.
        //
        // Maximum 24 hours current project-ku enough.
        if (expiryMinutes <= 0 ||
            expiryMinutes > 1440)
        {
            throw new InvalidOperationException(
                "Jwt:ExpiryMinutes must be between 1 and 1440.");
        }


        // =====================================================
        // JWT CLAIMS
        // =====================================================

        var claims =
            new List<Claim>
            {
                // ---------------------------------------------
                // USER ID
                // ---------------------------------------------

                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),


                // ---------------------------------------------
                // USER NAME
                // ---------------------------------------------

                new Claim(
                    ClaimTypes.Name,
                    user.FullName),


                // ---------------------------------------------
                // EMAIL
                // ---------------------------------------------

                new Claim(
                    ClaimTypes.Email,
                    user.Email),


                // ---------------------------------------------
                // ROLE
                // ---------------------------------------------

                new Claim(
                    ClaimTypes.Role,
                    user.Role),


                // ---------------------------------------------
                // TOKEN PURPOSE
                // ---------------------------------------------
                //
                // CRITICAL:
                //
                // Protected APIs access panna
                // purpose = access mandatory.
                new Claim(
                    "purpose",
                    "access"),


                // ---------------------------------------------
                // UNIQUE JWT ID
                // ---------------------------------------------

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid()
                        .ToString("N"))
            };


        // =====================================================
        // SIGNING KEY
        // =====================================================

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    key));


        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);


        // =====================================================
        // CREATE FINAL JWT
        // =====================================================

        var token =
            new JwtSecurityToken(
                issuer:
                    issuer,

                audience:
                    audience,

                claims:
                    claims,

                expires:
                    DateTime.UtcNow
                        .AddMinutes(
                            expiryMinutes),

                signingCredentials:
                    credentials);


        // =====================================================
        // RETURN TOKEN STRING
        // =====================================================

        return new JwtSecurityTokenHandler()
            .WriteToken(
                token);
    }
}