using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using PersonalDigitalVault.Api.Data;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Middleware;
using PersonalDigitalVault.Api.Repositories;
using PersonalDigitalVault.Api.Security;
using PersonalDigitalVault.Api.Services;
using PersonalDigitalVault.Api.Storage;

using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;


// =========================================================
// APPLICATION BUILDER
// =========================================================

var builder =
    WebApplication.CreateBuilder(args);


// =========================================================
// BASIC ASP.NET SERVICES
// =========================================================

// API Controllers enable pannum.
builder.Services
    .AddControllers();


// CurrentUserService JWT claims read panna
// IHttpContextAccessor use pannum.
builder.Services
    .AddHttpContextAccessor();


// =========================================================
// R7 - HTTP CLIENT FACTORY
// =========================================================
//
// Function:
//
// PayPal REST API
// PayPal OAuth
// PayPal Webhook Verification
//
// HTTP requests send panna IHttpClientFactory provide pannum.
//
// Security:
//
// ClientSecret inga hard-code panna maatom.
// User Secrets / Configuration-lendhu service read pannum.
//
builder.Services
    .AddHttpClient();


// =========================================================
// DATABASE
// =========================================================
//
// Input:
//
// ConnectionStrings:DefaultConnection
//
// Output:
//
// AppDbContext -> SQL Server
//
builder.Services
    .AddDbContext<AppDbContext>(
        options =>
        {
            options.UseSqlServer(
                builder.Configuration
                    .GetConnectionString(
                        "DefaultConnection"
                    )
            );
        }
    );


// =========================================================
// REPOSITORIES
// =========================================================


// ---------------------------------------------------------
// USER
// ---------------------------------------------------------

builder.Services.AddScoped<
    IUserRepository,
    UserRepository>();


// ---------------------------------------------------------
// FOLDER
// ---------------------------------------------------------

builder.Services.AddScoped<
    IFolderRepository,
    FolderRepository>();


// ---------------------------------------------------------
// DOCUMENT
// ---------------------------------------------------------

builder.Services.AddScoped<
    IDocumentRepository,
    DocumentRepository>();


// ---------------------------------------------------------
// CREDENTIAL
// ---------------------------------------------------------

builder.Services.AddScoped<
    ICredentialRepository,
    CredentialRepository>();


// ---------------------------------------------------------
// DOCUMENT SHARE
// ---------------------------------------------------------

builder.Services.AddScoped<
    IDocumentShareRepository,
    DocumentShareRepository>();


// ---------------------------------------------------------
// R7 - SUBSCRIPTION
// ---------------------------------------------------------
//
// Function:
//
// Subscriptions table:
// Add
// Update
// Find by User
// Find by PayPal SubscriptionId
// Admin-safe metadata query
//
builder.Services.AddScoped<
    ISubscriptionRepository,
    SubscriptionRepository>();


// =========================================================
// APPLICATION SERVICES
// =========================================================


// ---------------------------------------------------------
// AUTH
// ---------------------------------------------------------

builder.Services.AddScoped<
    IAuthService,
    AuthService>();


// ---------------------------------------------------------
// PROFILE
// ---------------------------------------------------------

builder.Services.AddScoped<
    IProfileService,
    ProfileService>();


// ---------------------------------------------------------
// FOLDER
// ---------------------------------------------------------

builder.Services.AddScoped<
    IFolderService,
    FolderService>();


// ---------------------------------------------------------
// DOCUMENT
// ---------------------------------------------------------

builder.Services.AddScoped<
    IDocumentService,
    DocumentService>();


// ---------------------------------------------------------
// CREDENTIAL
// ---------------------------------------------------------

builder.Services.AddScoped<
    ICredentialService,
    CredentialService>();


// ---------------------------------------------------------
// SEARCH
// ---------------------------------------------------------

builder.Services.AddScoped<
    ISearchService,
    SearchService>();


// ---------------------------------------------------------
// ADMIN
// ---------------------------------------------------------

builder.Services.AddScoped<
    IAdminService,
    AdminService>();


// ---------------------------------------------------------
// USER DASHBOARD
// ---------------------------------------------------------
//
// One authenticated aggregation endpoint for:
// counts, quota, safe subscription metadata, security state,
// recent owned-document metadata and recent folder metadata.
builder.Services.AddScoped<
    IDashboardService,
    DashboardService>();


// ---------------------------------------------------------
// EMAIL
// ---------------------------------------------------------

builder.Services.AddScoped<
    IEmailService,
    EmailService>();


// ---------------------------------------------------------
// DOCUMENT SHARING
// ---------------------------------------------------------

builder.Services.AddScoped<
    IDocumentShareService,
    DocumentShareService>();


// =========================================================
// R7 - PAYPAL / SUBSCRIPTION SERVICES
// =========================================================


// ---------------------------------------------------------
// PAYPAL REST SERVICE
// ---------------------------------------------------------
//
// Function:
//
// PayPal OAuth token
// Subscription verification
// Subscription cancellation
//
builder.Services.AddScoped<
    IPayPalService,
    PayPalService>();


// ---------------------------------------------------------
// SUBSCRIPTION BUSINESS SERVICE
// ---------------------------------------------------------
//
// Function:
//
// Current JWT User
//      ↓
// PayPal Subscription
//      ↓
// Plan verification
//      ↓
// Billing reference verification
//      ↓
// Local DB save/update
//
builder.Services.AddScoped<
    ISubscriptionService,
    SubscriptionService>();


// ---------------------------------------------------------
// PAYPAL WEBHOOK SERVICE
// ---------------------------------------------------------
//
// Function:
//
// PayPal webhook
//      ↓
// Signature verification
//      ↓
// PayPal authoritative status
//      ↓
// Local Subscription status update
//
builder.Services.AddScoped<
    IPayPalWebhookService,
    PayPalWebhookService>();



// =========================================================
// STRIPE PARALLEL SUBSCRIPTION SERVICES
// =========================================================
//
// PayPal remains enabled. Stripe is an additional provider
// with separate routes under /api/stripe/*.
//
builder.Services.AddScoped<
    IStripeService,
    StripeService>();

builder.Services.AddScoped<
    StripeSubscriptionSynchronizer>();

builder.Services.AddScoped<
    IStripeSubscriptionService,
    StripeSubscriptionService>();

builder.Services.AddScoped<
    IStripeWebhookService,
    StripeWebhookService>();


// ---------------------------------------------------------
// PREMIUM ENTITLEMENT SERVICE
// ---------------------------------------------------------
//
// Function:
//
// FREE:
//
// 20 Documents
// 50 MB
//
// PREMIUM:
//
// 200 Documents
// 500 MB
//
// Backend document upload-la actual quota enforce pannum.
//
builder.Services.AddScoped<
    ISubscriptionEntitlementService,
    SubscriptionEntitlementService>();


// ---------------------------------------------------------
// ADMIN BILLING METADATA SERVICE
// ---------------------------------------------------------
//
// Function:
//
// Admin:
// Safe / masked subscription metadata mattum.
//
// Admin cannot:
// Read payment credentials
// Access PayPal secrets
// Access vault private content
//
builder.Services.AddScoped<
    IAdminSubscriptionService,
    AdminSubscriptionService>();


// =========================================================
// DOCUMENT SHARE TOKEN SECURITY
// =========================================================
//
// Secure random invitation token generate/hash pannum.
//
// Raw token DB-la store panna maatom.
//
builder.Services.AddSingleton<
    DocumentShareTokenService>();


// =========================================================
// EMAIL OTP SECURITY
// =========================================================
//
// 6-digit OTP:
//
// Generate
// Hash
// Verify
//
builder.Services.AddScoped<
    EmailOtpService>();


// =========================================================
// TOTP / MFA SERVICES
// =========================================================


// ---------------------------------------------------------
// REGISTRATION / TOTP SETUP TOKEN
// ---------------------------------------------------------
//
// Email verification success
//      ↓
// Temporary totp_setup token
//
builder.Services.AddScoped<
    RegistrationTokenService>();


// ---------------------------------------------------------
// GOOGLE AUTHENTICATOR TOTP
// ---------------------------------------------------------
//
// Generate secret
// QR / URI
// Verify code
//
builder.Services.AddScoped<
    TotpService>();


// ---------------------------------------------------------
// TOTP SECRET PROTECTION
// ---------------------------------------------------------
//
// Authenticator secret plaintext-aa DB-la
// store pannaama encrypt pannum.
//
builder.Services.AddScoped<
    TotpSecretProtector>();


// ---------------------------------------------------------
// LOGIN MFA CHALLENGE TOKEN
// ---------------------------------------------------------
//
// Password success
//      ↓
// Temporary login_mfa token
//      ↓
// TOTP
//      ↓
// Final Access JWT
//
builder.Services.AddScoped<
    MfaChallengeTokenService>();
// =========================================================
// PASSWORD RESET TOKEN SECURITY
// =========================================================
//
// Function:
// Forgot Password-ku secure random reset token
// generate/hash/verify pannum.
//
// Input:
// Raw password reset token.
//
// Security:
// Service stateless.
//
// Raw reset token database-la store panna maatom.
// SHA-256 hash mattum database-la save pannuvom.
//
// Output:
// GenerateToken()
// HashToken()
// VerifyToken()
//
builder.Services.AddSingleton<
    PasswordResetTokenService>();

// =========================================================
// SECURITY HELPERS
// =========================================================


// ---------------------------------------------------------
// PASSWORD HASHING
// ---------------------------------------------------------

builder.Services.AddSingleton<
    PasswordHasher>();


// ---------------------------------------------------------
// FINAL JWT GENERATOR
// ---------------------------------------------------------

builder.Services.AddSingleton<
    JwtTokenGenerator>();


// ---------------------------------------------------------
// AES ENCRYPTION
// ---------------------------------------------------------
//
// Documents + Credentials encryption.
//
builder.Services.AddSingleton<
    AesEncryption>();


// ---------------------------------------------------------
// SHA-256
// ---------------------------------------------------------
//
// File integrity.
//
builder.Services.AddSingleton<
    FileHashService>();


// ---------------------------------------------------------
// CURRENT USER
// ---------------------------------------------------------
//
// JWT-lendhu:
//
// UserId
// Role
//
// read pannum.
//
builder.Services.AddScoped<
    CurrentUserService>();


// ---------------------------------------------------------
// PROTECTED FILE STORAGE
// ---------------------------------------------------------
//
// Encrypted .vault files
// wwwroot-ku veliya save pannum.
//
builder.Services.AddSingleton<
    FileStorageService>();


// =========================================================
// GOOGLE DRIVE ENCRYPTED BACKUP
// =========================================================
//
// Google OAuth state signing + least-privilege Drive appData
// client + encrypted package orchestration + scheduled backups.
// Existing vault encryption and ownership boundaries stay intact.
//
builder.Services.AddSingleton<
    GoogleOAuthStateService>();

builder.Services.AddScoped<
    GoogleDriveClient>();

builder.Services.AddScoped<
    VaultBackupPackageService>();

builder.Services.AddScoped<
    IGoogleDriveBackupService,
    GoogleDriveBackupService>();

builder.Services.AddHostedService<
    GoogleDriveAutoBackupWorker>();


// =========================================================
// DATABASE SEEDER
// =========================================================
//
// Default Admin ensure pannum.
//
builder.Services.AddScoped<
    DbSeeder>();


// =========================================================
// SECURITY CONFIGURATION VALIDATION
// =========================================================


// ---------------------------------------------------------
// FINAL JWT KEY
// ---------------------------------------------------------

string jwtKey =
    GetRequiredSetting(
        builder.Configuration,
        "Jwt:Key"
    );


// ---------------------------------------------------------
// TEMPORARY MFA TOKEN KEY
// ---------------------------------------------------------

string mfaTokenKey =
    GetRequiredSetting(
        builder.Configuration,
        "Security:MfaTokenKey"
    );


// ---------------------------------------------------------
// JWT ISSUER
// ---------------------------------------------------------

string jwtIssuer =
    GetRequiredSetting(
        builder.Configuration,
        "Jwt:Issuer"
    );


// ---------------------------------------------------------
// JWT AUDIENCE
// ---------------------------------------------------------

string jwtAudience =
    GetRequiredSetting(
        builder.Configuration,
        "Jwt:Audience"
    );


// ---------------------------------------------------------
// MFA ISSUER
// ---------------------------------------------------------
//
// RegistrationTokenService + MfaChallengeTokenService
// use pannum.
//
_ =
    GetRequiredSetting(
        builder.Configuration,
        "Security:MfaIssuer"
    );


// ---------------------------------------------------------
// MFA AUDIENCE
// ---------------------------------------------------------

_ =
    GetRequiredSetting(
        builder.Configuration,
        "Security:MfaAudience"
    );


// =========================================================
// KEY LENGTH VALIDATION
// =========================================================
//
// Minimum 32 bytes.
//
if (
    Encoding.UTF8
        .GetByteCount(
            jwtKey
        )
    <
    32
)
{
    throw new InvalidOperationException(
        "Jwt:Key must contain at least 32 bytes."
    );
}


if (
    Encoding.UTF8
        .GetByteCount(
            mfaTokenKey
        )
    <
    32
)
{
    throw new InvalidOperationException(
        "Security:MfaTokenKey must contain at least 32 bytes."
    );
}


// =========================================================
// JWT / MFA KEY SEPARATION
// =========================================================
//
// CRITICAL:
//
// Final Access JWT key
//
// and
//
// Temporary MFA token key
//
// SAME value-a irukka koodathu.
//
if (
    string.Equals(
        jwtKey,
        mfaTokenKey,
        StringComparison.Ordinal
    )
)
{
    throw new InvalidOperationException(
        "Jwt:Key and Security:MfaTokenKey must be different."
    );
}


// =========================================================
// JWT AUTHENTICATION
// =========================================================

builder.Services
    .AddAuthentication(
        JwtBearerDefaults
            .AuthenticationScheme
    )
    .AddJwtBearer(
        options =>
        {
            // =================================================
            // STANDARD JWT VALIDATION
            // =================================================

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    // -----------------------------------------
                    // ISSUER
                    // -----------------------------------------

                    ValidateIssuer =
                        true,

                    ValidIssuer =
                        jwtIssuer,


                    // -----------------------------------------
                    // AUDIENCE
                    // -----------------------------------------

                    ValidateAudience =
                        true,

                    ValidAudience =
                        jwtAudience,


                    // -----------------------------------------
                    // EXPIRY
                    // -----------------------------------------

                    ValidateLifetime =
                        true,


                    // -----------------------------------------
                    // SIGNATURE
                    // -----------------------------------------

                    ValidateIssuerSigningKey =
                        true,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8
                                .GetBytes(
                                    jwtKey
                                )
                        ),


                    // -----------------------------------------
                    // NO EXTRA EXPIRY GRACE
                    // -----------------------------------------

                    ClockSkew =
                        TimeSpan.Zero
                };


            // =================================================
            // DATABASE-AWARE JWT VALIDATION
            // =================================================
            //
            // JWT cryptographically correct-aa irundhaalum:
            //
            // User disabled?
            // Role changed?
            // Email verification removed?
            // TOTP disabled?
            //
            // current DB state verify pannuvom.
            //
            options.Events =
                new JwtBearerEvents
                {
                    OnTokenValidated =
                        async context =>
                        {
                            ClaimsPrincipal?
                                principal =
                                    context.Principal;


                            // =================================
                            // TOKEN PURPOSE
                            // =================================
                            //
                            // Protected APIs-ku:
                            //
                            // purpose = access
                            //
                            // mattum allowed.
                            //
                            // login_mfa / totp_setup temporary
                            // tokens reject aagum.
                            string? purpose =
                                principal?
                                    .FindFirst(
                                        "purpose"
                                    )?
                                    .Value;


                            if (
                                !string.Equals(
                                    purpose,
                                    "access",
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                context.Fail(
                                    "Invalid token."
                                );

                                return;
                            }


                            // =================================
                            // USER ID
                            // =================================

                            string? userIdText =
                                principal?
                                    .FindFirst(
                                        ClaimTypes
                                            .NameIdentifier
                                    )?
                                    .Value;


                            if (
                                !int.TryParse(
                                    userIdText,
                                    out int userId
                                )
                                ||
                                userId <= 0
                            )
                            {
                                context.Fail(
                                    "Invalid token."
                                );

                                return;
                            }


                            // =================================
                            // CURRENT DATABASE USER
                            // =================================

                            IUserRepository
                                userRepository =
                                    context
                                        .HttpContext
                                        .RequestServices
                                        .GetRequiredService<
                                            IUserRepository
                                        >();


                            var currentUser =
                                await userRepository
                                    .GetByIdAsync(
                                        userId
                                    );


                            // User deleted / missing / disabled.
                            if (
                                currentUser == null
                                ||
                                !currentUser.IsActive
                            )
                            {
                                context.Fail(
                                    "Invalid token."
                                );

                                return;
                            }


                            // =================================
                            // ROLE CHECK
                            // =================================

                            string? tokenRole =
                                principal?
                                    .FindFirst(
                                        ClaimTypes.Role
                                    )?
                                    .Value;


                            bool validDatabaseRole =
                                string.Equals(
                                    currentUser.Role,
                                    "User",
                                    StringComparison
                                        .OrdinalIgnoreCase
                                )
                                ||
                                string.Equals(
                                    currentUser.Role,
                                    "Admin",
                                    StringComparison
                                        .OrdinalIgnoreCase
                                );


                            if (
                                !validDatabaseRole
                                ||
                                string.IsNullOrWhiteSpace(
                                    tokenRole
                                )
                                ||
                                !string.Equals(
                                    tokenRole,
                                    currentUser.Role,
                                    StringComparison
                                        .OrdinalIgnoreCase
                                )
                            )
                            {
                                context.Fail(
                                    "Invalid token."
                                );

                                return;
                            }


                            // =================================
                            // NORMAL USER MFA STATE
                            // =================================

                            bool isNormalUser =
                                string.Equals(
                                    currentUser.Role,
                                    "User",
                                    StringComparison
                                        .OrdinalIgnoreCase
                                );


                            if (isNormalUser)
                            {
                                // Email verified-a irukkanum.
                                if (
                                    !currentUser
                                        .IsEmailVerified
                                )
                                {
                                    context.Fail(
                                        "Invalid token."
                                    );

                                    return;
                                }


                                // Authenticator TOTP enabled.
                                if (
                                    !currentUser
                                        .IsTotpEnabled
                                    ||
                                    string
                                        .IsNullOrWhiteSpace(
                                            currentUser
                                                .TotpSecretEncrypted
                                        )
                                )
                                {
                                    context.Fail(
                                        "Invalid token."
                                    );

                                    return;
                                }
                            }

                            // =================================
                            // ADMIN LOGIN DESIGN
                            // =================================
                            //
                            // Current frozen design:
                            //
                            // Admin:
                            // Password
                            //      ↓
                            // Direct final JWT
                            //
                            // Admin-ku TOTP requirement inga
                            // force panna maatom.
                        }
                };
        }
    );


// =========================================================
// AUTHORIZATION
// =========================================================

builder.Services
    .AddAuthorization();


// =========================================================
// R4 - AUTH ABUSE RATE LIMITING
// =========================================================
//
// Sensitive anonymous auth endpoints-ku
// IP-based fixed window rate limit.
//
builder.Services
    .AddRateLimiter(
        options =>
        {
            options.GlobalLimiter =
                PartitionedRateLimiter
                    .Create<
                        HttpContext,
                        string
                    >(
                        httpContext =>
                        {
                            // =============================
                            // POST REQUESTS MATTUM
                            // =============================

                            if (
                                !HttpMethods
                                    .IsPost(
                                        httpContext
                                            .Request
                                            .Method
                                    )
                            )
                            {
                                return RateLimitPartition
                                    .GetNoLimiter(
                                        "non-post-request"
                                    );
                            }


                            string path =
                                httpContext
                                    .Request
                                    .Path
                                    .Value?
                                    .ToLowerInvariant()
                                ??
                                string.Empty;


                            string ipAddress =
                                httpContext
                                    .Connection
                                    .RemoteIpAddress?
                                    .ToString()
                                ??
                                "unknown";


                            // =============================
                            // REGISTER
                            // =============================

                            if (
                                path ==
                                "/api/auth/register"
                            )
                            {
                                return CreateFixedWindowPartition(
                                    $"{ipAddress}|register",
                                    5,
                                    TimeSpan
                                        .FromMinutes(
                                            10
                                        )
                                );
                            }


                            // =============================
                            // EMAIL OTP VERIFY
                            // =============================

                            if (
                                path ==
                                "/api/auth/verify-email"
                            )
                            {
                                return CreateFixedWindowPartition(
                                    $"{ipAddress}|verify-email",
                                    10,
                                    TimeSpan
                                        .FromMinutes(
                                            5
                                        )
                                );
                            }


                            // =============================
                            // EMAIL OTP RESEND
                            // =============================

                            if (
                                path ==
                                "/api/auth/resend-email-otp"
                            )
                            {
                                return CreateFixedWindowPartition(
                                    $"{ipAddress}|resend-email",
                                    5,
                                    TimeSpan
                                        .FromMinutes(
                                            10
                                        )
                                );
                            }


                            // =================================================
                            // FORGOT PASSWORD
                            // =================================================
                            //
                            // Function:
                            // Password reset email request abuse
                            // prevent panna.
                            //
                            // Maximum:
                            // 5 requests / 10 minutes / IP.
                            //
                            // Service level-la additionally:
                            // 60-second per-account cooldown irukku.
                            //
                            if (path ==
                                "/api/auth/forgot-password")
                            {
                                return CreateFixedWindowPartition(
                                    $"{ipAddress}|forgot-password",
                                    permitLimit: 5,
                                    window:
                                        TimeSpan.FromMinutes(10));
                            }


                            // =================================================
                            // RESET PASSWORD
                            // =================================================
                            //
                            // Function:
                            // Invalid reset token repeated brute-force
                            // requests reduce panna.
                            //
                            // Maximum:
                            // 5 requests / 10 minutes / IP.
                            //
                            if (path ==
                                "/api/auth/reset-password")
                            {
                                return CreateFixedWindowPartition(
                                    $"{ipAddress}|reset-password",
                                    permitLimit: 5,
                                    window:
                                        TimeSpan.FromMinutes(10));
                            }

                            // =============================
                            // TOTP SETUP
                            // =============================

                            if (
                                path ==
                                "/api/auth/setup-totp"
                            )
                            {
                                return CreateFixedWindowPartition(
                                    $"{ipAddress}|setup-totp",
                                    10,
                                    TimeSpan
                                        .FromMinutes(
                                            10
                                        )
                                );
                            }


                            // =============================
                            // TOTP SETUP VERIFY
                            // =============================

                            if (
                                path ==
                                "/api/auth/verify-totp-setup"
                            )
                            {
                                return CreateFixedWindowPartition(
                                    $"{ipAddress}|verify-totp-setup",
                                    5,
                                    TimeSpan
                                        .FromMinutes(
                                            5
                                        )
                                );
                            }


                            // =============================
                            // PASSWORD LOGIN
                            // =============================

                            if (
                                path ==
                                "/api/auth/login"
                            )
                            {
                                return CreateFixedWindowPartition(
                                    $"{ipAddress}|login",
                                    5,
                                    TimeSpan
                                        .FromMinutes(
                                            1
                                        )
                                );
                            }


                            // =============================
                            // LOGIN TOTP VERIFY
                            // =============================

                            if (
                                path ==
                                "/api/auth/verify-login-totp"
                            )
                            {
                                return CreateFixedWindowPartition(
                                    $"{ipAddress}|verify-login-totp",
                                    5,
                                    TimeSpan
                                        .FromMinutes(
                                            5
                                        )
                                );
                            }


                            // Other endpoints-ku
                            // R4 auth limiter apply aagaathu.
                            return RateLimitPartition
                                .GetNoLimiter(
                                    "non-auth-endpoint"
                                );
                        }
                    );


            // =================================================
            // 429 RESPONSE
            // =================================================

            options.OnRejected =
                async (
                    context,
                    cancellationToken
                ) =>
                {
                    context
                        .HttpContext
                        .Response
                        .StatusCode =
                            StatusCodes
                                .Status429TooManyRequests;


                    await context
                        .HttpContext
                        .Response
                        .WriteAsJsonAsync(
                            new
                            {
                                message =
                                    "Too many requests. Please try again later."
                            },
                            cancellationToken
                        );
                };
        }
    );


// =========================================================
// BUILD APPLICATION
// =========================================================

var app =
    builder.Build();


// =========================================================
// DEFAULT ADMIN SEED
// =========================================================
//
// Application startup time:
// Default Admin account ensure pannum.
//
using (
    var scope =
        app.Services
            .CreateScope()
)
{
    DbSeeder seeder =
        scope
            .ServiceProvider
            .GetRequiredService<
                DbSeeder
            >();


    await seeder
        .SeedAsync();
}


// =========================================================
// HTTP REQUEST PIPELINE
// =========================================================


// ---------------------------------------------------------
// GLOBAL ERROR HANDLING
// ---------------------------------------------------------
//
// Stack traces / secrets frontend-ku expose pannaama
// safe error response.
//
app.UseMiddleware<
    ExceptionMiddleware>();


// ---------------------------------------------------------
// HTTPS
// ---------------------------------------------------------

app.UseHttpsRedirection();


// ---------------------------------------------------------
// DEFAULT FRONTEND FILE
// ---------------------------------------------------------

app.UseDefaultFiles();


// ---------------------------------------------------------
// STATIC FILES
// ---------------------------------------------------------
//
// wwwroot:
// HTML
// CSS
// JavaScript
//
// SecureStorage wwwroot-kulla illa.
//
app.UseStaticFiles();


// ---------------------------------------------------------
// RATE LIMITING
// ---------------------------------------------------------

app.UseRateLimiter();


// ---------------------------------------------------------
// JWT AUTHENTICATION
// ---------------------------------------------------------

app.UseAuthentication();


// ---------------------------------------------------------
// AUTHORIZATION
// ---------------------------------------------------------

app.UseAuthorization();


// ---------------------------------------------------------
// API CONTROLLERS
// ---------------------------------------------------------

app.MapControllers();


// =========================================================
// RUN APPLICATION
// =========================================================

app.Run();


// =========================================================
// LOCAL HELPER FUNCTIONS
// =========================================================


// ---------------------------------------------------------
// REQUIRED CONFIG VALUE
// ---------------------------------------------------------
//
// Function:
//
// Missing critical config-na application
// startup fail pannum.
//
static string GetRequiredSetting(
    IConfiguration configuration,
    string key)
{
    string? value =
        configuration[
            key
        ];


    if (
        string.IsNullOrWhiteSpace(
            value
        )
    )
    {
        throw new InvalidOperationException(
            $"{key} is missing."
        );
    }


    return value;
}


// ---------------------------------------------------------
// FIXED WINDOW RATE LIMITER
// ---------------------------------------------------------
//
// Input:
//
// partitionKey
// permitLimit
// window
//
// Output:
//
// Fixed window request limiter.
//
static RateLimitPartition<string>
    CreateFixedWindowPartition(
        string partitionKey,
        int permitLimit,
        TimeSpan window)
{
    return RateLimitPartition
        .GetFixedWindowLimiter(
            partitionKey,

            _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit =
                        permitLimit,

                    Window =
                        window,

                    QueueProcessingOrder =
                        QueueProcessingOrder
                            .OldestFirst,

                    // Limit exceed requests queue panna maatom.
                    QueueLimit =
                        0,

                    AutoReplenishment =
                        true
                }
        );
}
