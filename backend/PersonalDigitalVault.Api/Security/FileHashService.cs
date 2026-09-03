using System.Security.Cryptography;

namespace PersonalDigitalVault.Api.Security;

public class FileHashService
{
    // =========================================================
    // SHA-256 HASH
    // =========================================================

    // Function:
    // File bytes-ku SHA-256 integrity hash create pannum.
    //
    // Input:
    // data = original/decrypted file byte[].
    //
    // Reason:
    // Upload panna original file later modify/corrupt
    // aagirukka nu verify panna.
    //
    // Output:
    // SHA-256 hash hexadecimal string.
    //
    // Example:
    // A1B2C3D4...
    //
    // Security:
    // SHA-256 inga password hashing-ku use pannala.
    // Document integrity verification-ku mattum use pannrom.
    public string Sha256(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(
                nameof(data));
        }

        // SHA-256 hash calculate pannrom.
        byte[] hashBytes =
            SHA256.HashData(data);

        // Database-la easy-a store/compare panna
        // hexadecimal string-a convert pannrom.
        return Convert.ToHexString(
            hashBytes);
    }
}