using System.Security.Cryptography;
using System.Text;

namespace PersonalDigitalVault.Api.Security;

public class EmailOtpService
{
    // =========================================================
    // OTP PEPPER KEY
    // =========================================================

    // User Secrets-la irukkura secret pepper bytes.
    //
    // Reason:
    // 6-digit OTP small search space.
    // Plain SHA-256 mattum use panna koodathu.
    //
    // HMAC-SHA256 secret key-oda OTP hash pannum.
    private readonly byte[] _pepperKey;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Security:OtpPepper configuration-la read pannum.
    //
    // Input:
    // IConfiguration
    //
    // Reason:
    // OTP HMAC hashing-ku secret key thevai.
    //
    // Output:
    // Pepper bytes _pepperKey-la save aagum.
    //
    // Security:
    // Missing pepper irundha OTP security continue panna
    // koodathu.
    public EmailOtpService(
        IConfiguration configuration)
    {
        string? pepper =
            configuration[
                "Security:OtpPepper"
            ];


        if (string.IsNullOrWhiteSpace(
                pepper))
        {
            throw new InvalidOperationException(
                "OTP security key is missing.");
        }


        _pepperKey =
            Encoding.UTF8.GetBytes(
                pepper);
    }


    // =========================================================
    // OTP GENERATE
    // =========================================================

    // Function:
    // Secure random 6-digit OTP generate pannum.
    //
    // Input:
    // None.
    //
    // Output:
    // Exactly 6 digit string.
    //
    // Example:
    // 004281
    //
    // Security:
    // Normal Random class use panna maatom.
    // Cryptographic RNG use pannrom.
    public string GenerateOtp()
    {
        int number =
            RandomNumberGenerator.GetInt32(
                0,
                1_000_000);


        // Leading zero irundhaalum
        // six digits preserve pannum.
        return number.ToString(
            "D6");
    }


    // =========================================================
    // OTP HASH
    // =========================================================

    // Function:
    // Plain OTP-a HMAC-SHA256 use panni hash pannum.
    //
    // Input:
    // Exactly 6-digit OTP.
    //
    // Reason:
    // Plain OTP database-la store panna koodathu.
    //
    // Output:
    // Base64 HMAC-SHA256 hash.
    //
    // Security:
    // Security:OtpPepper secret key use pannrom.
    public string HashOtp(
        string otp)
    {
        string normalizedOtp =
            NormalizeAndValidateOtp(
                otp);


        byte[] otpBytes =
            Encoding.UTF8.GetBytes(
                normalizedOtp);


        using var hmac =
            new HMACSHA256(
                _pepperKey);


        byte[] hash =
            hmac.ComputeHash(
                otpBytes);


        return Convert.ToBase64String(
            hash);
    }


    // =========================================================
    // OTP VERIFY
    // =========================================================

    // Function:
    // User entered OTP stored HMAC hash-oda match aagutha
    // verify pannum.
    //
    // Input:
    // otp        = user entered OTP
    // storedHash = database HMAC hash
    //
    // Output:
    // true  = correct
    // false = wrong / malformed / corrupted hash
    //
    // Security:
    // Fixed-time comparison use pannrom.
    public bool VerifyOtp(
        string otp,
        string storedHash)
    {
        if (string.IsNullOrWhiteSpace(
                otp) ||
            string.IsNullOrWhiteSpace(
                storedHash))
        {
            return false;
        }


        string normalizedOtp;


        try
        {
            normalizedOtp =
                NormalizeAndValidateOtp(
                    otp);
        }
        catch (ArgumentException)
        {
            // Invalid OTP format-ku exception frontend-ku
            // leak pannaama simply false.
            return false;
        }


        byte[] storedHashBytes;


        try
        {
            storedHashBytes =
                Convert.FromBase64String(
                    storedHash);
        }
        catch (FormatException)
        {
            // Database value corrupted / malformed.
            // Request crash panna koodathu.
            return false;
        }


        // HMAC-SHA256 output 256 bits = 32 bytes. :contentReference[oaicite:4]{index=4}
        if (storedHashBytes.Length != 32)
        {
            return false;
        }


        byte[] otpBytes =
            Encoding.UTF8.GetBytes(
                normalizedOtp);


        byte[] newHashBytes;


        using (
            var hmac =
                new HMACSHA256(
                    _pepperKey)
        )
        {
            newHashBytes =
                hmac.ComputeHash(
                    otpBytes);
        }


        // Fixed-time byte comparison.
        return CryptographicOperations
            .FixedTimeEquals(
                newHashBytes,
                storedHashBytes);
    }


    // =========================================================
    // OTP FORMAT VALIDATION
    // =========================================================

    // Function:
    // OTP exactly 6 ASCII digits-aa check pannum.
    //
    // Input:
    // OTP string.
    //
    // Reason:
    // Spaces, letters, Unicode digits, unexpected input
    // accept panna koodathu.
    //
    // Output:
    // Valid normalized 6-digit OTP.
    //
    // Invalid:
    // ArgumentException.
    private static string NormalizeAndValidateOtp(
        string otp)
    {
        if (string.IsNullOrWhiteSpace(
                otp))
        {
            throw new ArgumentException(
                "Invalid OTP.");
        }


        string normalizedOtp =
            otp.Trim();


        // Exactly 6 characters mandatory.
        if (normalizedOtp.Length != 6)
        {
            throw new ArgumentException(
                "Invalid OTP.");
        }


        // ASCII 0-9 mattum allow.
        foreach (char character in normalizedOtp)
        {
            if (character < '0' ||
                character > '9')
            {
                throw new ArgumentException(
                    "Invalid OTP.");
            }
        }


        return normalizedOtp;
    }
}