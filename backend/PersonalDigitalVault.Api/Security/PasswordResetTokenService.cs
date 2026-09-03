using System.Security.Cryptography;
using System.Text;

namespace PersonalDigitalVault.Api.Security;

public class PasswordResetTokenService
{
    // =========================================================
    // GENERATE RESET TOKEN
    // =========================================================
    //
    // Function:
    // Password reset-ku strong random token generate pannum.
    //
    // Input:
    // Input thevai illa.
    //
    // Reason:
    // Guess panna mudiyatha secure reset link create panna.
    //
    // Security:
    // RandomNumberGenerator cryptographically secure
    // random bytes generate pannum.
    //
    // 32 bytes = 256 bits random data.
    //
    // Output:
    // Browser URL-la safely use panna mudiyura
    // Base64 URL-safe token return pannum.

    public string GenerateToken()
    {
        // 32 cryptographically secure random bytes create pannum.
        byte[] randomBytes =
            RandomNumberGenerator.GetBytes(32);


        // Normal Base64 string create pannum.
        string token =
            Convert.ToBase64String(
                randomBytes);


        // =====================================================
        // BASE64 -> URL SAFE
        // =====================================================
        //
        // Normal Base64-la:
        // + / =
        //
        // characters URL-la problem create panna chance irukku.
        //
        // So:
        // + -> -
        // / -> _
        // = remove
        //
        // Idhu Base64Url-style token.

        token =
            token
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');


        return token;
    }


    // =========================================================
    // HASH RESET TOKEN
    // =========================================================
    //
    // Function:
    // Raw reset token-a SHA-256 hash pannum.
    //
    // Input:
    // Email link-la use aagura raw token.
    //
    // Reason:
    // Raw reset token database-la store panna koodathu.
    //
    // Database leak aana attacker direct reset link use
    // panna mudiyama reduce pannum.
    //
    // Output:
    // 64-character SHA-256 HEX hash.

    public string HashToken(
        string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException(
                "Reset token is required.",
                nameof(token));
        }


        // Token string -> bytes
        byte[] tokenBytes =
            Encoding.UTF8.GetBytes(
                token);


        // SHA-256 hash create pannum.
        byte[] hashBytes =
            SHA256.HashData(
                tokenBytes);


        // Database-la easy-aa save panna
        // HEX string-aa convert pannum.
        return Convert.ToHexString(
            hashBytes);
    }


    // =========================================================
    // VERIFY RESET TOKEN
    // =========================================================
    //
    // Function:
    // User submit panna token database-la save panna
    // hash-oda match aagutha nu verify pannum.
    //
    // Input:
    // token      -> browser/email-la irundhu varum raw token
    // storedHash -> database-la save panna SHA-256 hash
    //
    // Reason:
    // Raw token DB-la store pannaama verification panna.
    //
    // Output:
    // true  -> token correct
    // false -> token wrong / invalid

    public bool VerifyToken(
        string token,
        string storedHash)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }


        try
        {
            // User submit panna raw token-a
            // same SHA-256 method use panni hash pannum.
            string submittedHash =
                HashToken(
                    token);


            // HEX strings -> byte arrays
            byte[] submittedBytes =
                Convert.FromHexString(
                    submittedHash);


            byte[] storedBytes =
                Convert.FromHexString(
                    storedHash);


            // Different size-na match impossible.
            if (submittedBytes.Length !=
                storedBytes.Length)
            {
                return false;
            }


            // =================================================
            // CONSTANT-TIME COMPARISON
            // =================================================
            //
            // Normal string comparison instead of
            // FixedTimeEquals use pannrom.
            //
            // Reason:
            // Timing information leak reduce panna.

            return CryptographicOperations
                .FixedTimeEquals(
                    submittedBytes,
                    storedBytes);
        }
        catch
        {
            // Invalid token/hash format vandhaalum
            // application crash panna koodathu.
            return false;
        }
    }
}