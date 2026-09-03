namespace PersonalDigitalVault.Api.Entities;

public class User
{
    // =========================================================
    // BASIC USER DETAILS
    // =========================================================

    // Database primary key.
    public int Id { get; set; }


    // User full name.
    public string FullName { get; set; }
        = string.Empty;


    // Login + email verification address.
    public string Email { get; set; }
        = string.Empty;


    // Plain password database-la save panna maatom.
    // Secure password hash mattum save pannuvom.
    public string PasswordHash { get; set; }
        = string.Empty;


    // Current roles:
    // User
    // Admin
    public string Role { get; set; }
        = "User";


    // true:
    // Account active.
    //
    // false:
    // Admin disable pannina account.
    public bool IsActive { get; set; }
        = true;


    // Stripe Customer id used only as an external billing identity reference.
    // Card/payment credentials are never stored in PDV.
    public string? StripeCustomerId { get; set; }


    // Account created date/time.
    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;


    // =========================================================
    // EMAIL VERIFICATION
    // =========================================================

    // false:
    // Email verification complete aagala.
    //
    // true:
    // Email OTP successfully verified.
    public bool IsEmailVerified { get; set; }
        = false;


    // Email OTP-oda HMAC-SHA256 hash.
    //
    // Plain OTP database-la save panna maatom.
    public string? EmailOtpHash { get; set; }


    // Current OTP expiry time.
    //
    // Example:
    // OTP generated time + 5 minutes.
    public DateTime? EmailOtpExpiresAt { get; set; }


    // Current OTP-ku wrong attempts count.
    //
    // Maximum currently 5.
    public int EmailOtpAttempts { get; set; }
        = 0;


    // OTP resend cooldown calculate panna
    // last OTP send time.
    public DateTime? EmailOtpLastSentAt { get; set; }


    // =========================================================
    // TOTP / TWO-FACTOR AUTHENTICATION
    // =========================================================

    // Google Authenticator compatible TOTP secret.
    //
    // IMPORTANT:
    // Secret plaintext database-la save panna maatom.
    //
    // TotpSecretProtector AES-GCM use panni
    // encrypt pannina value mattum inga save aagum.
    public string? TotpSecretEncrypted { get; set; }


    // false:
    // Authenticator setup complete aagala.
    //
    // true:
    // Initial TOTP successfully verified.
    public bool IsTotpEnabled { get; set; }
        = false;


    // =========================================================
    // TOTP REPLAY PROTECTION
    // =========================================================

    // Function:
    // Last successfully accepted TOTP time-step
    // database-la remember pannum.
    //
    // Example:
    //
    // Authenticator code belongs to time-step 59124501
    //       ↓
    // Verify success
    //       ↓
    // LastTotpTimeStepUsed = 59124501
    //
    // Same code/time-step again:
    //
    // matchedTimeStep <= LastTotpTimeStepUsed
    //       ↓
    // REJECT
    //
    // Input:
    // TotpService VerifyCode(..., out matchedTimeStep)
    // return pannura time-step.
    //
    // Reason:
    // Same valid 6-digit authenticator code replay
    // panna mudiyama prevent panna.
    //
    // null:
    // User innum successful TOTP use pannala.
    //
    // Security:
    // TOTP secret illa.
    // OTP code-um illa.
    // Time-step number mattum store pannrom.
    public long? LastTotpTimeStepUsed { get; set; }


    // =========================================================
    // PASSWORD RESET / FORGOT PASSWORD
    // =========================================================

    // Function:
    // Forgot Password request generate panna
    // reset token-oda SHA-256 hash save pannum.
    //
    // IMPORTANT:
    // Email-la send panna raw reset token
    // database-la save panna maatom.
    //
    // Example:
    //
    // Raw token:
    // AbCd1234....
    //
    //        ↓ SHA-256
    //
    // Database:
    // 8A15F3....
    //
    // Input:
    // PasswordResetTokenService.HashToken(...)
    //
    // Reason:
    // Database compromise aanaalum
    // raw password reset link attacker-ku
    // direct-aa kidaikka koodathu.
    //
    // null:
    // Active password reset request illa.
    public string? PasswordResetTokenHash { get; set; }


    // Function:
    // Current password reset token
    // eppo expire aagum nu store pannum.
    //
    // Example:
    //
    // Reset request:
    // 10:00 AM
    //
    // Expiry:
    // 10:15 AM
    //
    // Reset attempt expiry-ku apram vandha:
    // REJECT.
    //
    // null:
    // Active reset token illa.
    public DateTime? PasswordResetTokenExpiresAt { get; set; }


    // Function:
    // Last password-reset email
    // eppo send pannom nu remember pannum.
    //
    // Reason:
    // User repeated-aa Forgot Password
    // click panna email spam prevent panna.
    //
    // Example:
    //
    // First request:
    // 10:00:00
    //
    // Next request:
    // 10:00:20
    //
    // Cooldown complete aagala-na
    // new mail send panna maatom.
    //
    // null:
    // Password reset email
    // ithuvaraikkum send pannala.
    public DateTime? PasswordResetLastSentAt { get; set; }


    // =========================================================
    // NAVIGATION PROPERTIES
    // =========================================================

    // User own folders.
    public ICollection<Folder> Folders { get; set; }
        = new List<Folder>();


    // User own uploaded documents.
    public ICollection<Document> Documents { get; set; }
        = new List<Document>();


    // User own encrypted credential records.
    public ICollection<Credential> Credentials { get; set; }
        = new List<Credential>();


    // =========================================================
    // DOCUMENT SHARING
    // =========================================================

    // Indha user document owner-aa irundhu
    // other registered users-ku share panna records.
    public ICollection<DocumentShare> SentDocumentShares
    {
        get;
        set;
    }
        = new List<DocumentShare>();


    // Other registered users indha user-ku
    // share panna document records.
    public ICollection<DocumentShare> ReceivedDocumentShares
    {
        get;
        set;
    }
        = new List<DocumentShare>();
}