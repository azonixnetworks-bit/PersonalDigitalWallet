using System.Security.Cryptography;
using System.Text;

namespace PersonalDigitalVault.Api.Security;

public class DocumentShareTokenService
{
    // =====================================================
    // SECURE INVITATION TOKEN GENERATE
    // =====================================================
    //
    // INPUT:
    // None
    //
    // REASON:
    // Email invitation-ku guess panna mudiyatha
    // strong random token venum.
    //
    // OUTPUT:
    // URL-safe random token.
    //
    public string GenerateToken()
    {
        // 32 bytes = 256 bits random data
        var bytes =
            RandomNumberGenerator
                .GetBytes(32);


        // Base64 string create pannuvom
        var token =
            Convert.ToBase64String(
                bytes
            );


        // URL-safe format
        //
        // + replace with -
        // / replace with _
        // = padding remove
        //
        token =
            token
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');


        return token;
    }


    // =====================================================
    // TOKEN HASH
    // =====================================================
    //
    // INPUT:
    // Raw invitation token
    //
    // REASON:
    // Plain invitation token database-la
    // save panna koodathu.
    //
    // OUTPUT:
    // SHA-256 hexadecimal hash.
    //
    public string HashToken(
        string token)
    {
        if (
            string.IsNullOrWhiteSpace(
                token
            )
        )
        {
            throw new ArgumentException(
                "Invitation token is required."
            );
        }


        var bytes =
            Encoding.UTF8
                .GetBytes(
                    token
                );


        var hash =
            SHA256.HashData(
                bytes
            );


        return Convert
            .ToHexString(
                hash
            );
    }
}