using System.Security.Cryptography;
using System.Text;

namespace PersonalDigitalVault.Api.Security;

public class TotpSecretProtector
{
    private readonly byte[] _key;


    public TotpSecretProtector(
        IConfiguration configuration)
    {
        var keyBase64 =
            configuration[
                "Security:AesKeyBase64"
            ];


        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException(
                "AES key missing."
            );
        }


        _key =
            Convert.FromBase64String(
                keyBase64
            );


        // AES-256-ku 32 bytes key venum
        if (_key.Length != 32)
        {
            throw new InvalidOperationException(
                "AES key must be 32 bytes."
            );
        }
    }


    // =====================================================
    // ENCRYPT TOTP SECRET
    // =====================================================
    //
    // INPUT:
    // Base32 TOTP secret
    //
    // OUTPUT:
    // Encrypted Base64 string
    //
    public string Encrypt(
        string plainText)
    {
        var plainBytes =
            Encoding.UTF8.GetBytes(
                plainText
            );


        // AES-GCM standard nonce size
        var nonce =
            RandomNumberGenerator
                .GetBytes(12);


        var cipherBytes =
            new byte[
                plainBytes.Length
            ];


        var tag =
            new byte[16];


        using var aes =
            new AesGcm(
                _key,
                16
            );


        aes.Encrypt(
            nonce,
            plainBytes,
            cipherBytes,
            tag
        );


        // nonce + cipher + tag
        var result =
            new byte[
                nonce.Length +
                cipherBytes.Length +
                tag.Length
            ];


        Buffer.BlockCopy(
            nonce,
            0,
            result,
            0,
            nonce.Length
        );


        Buffer.BlockCopy(
            cipherBytes,
            0,
            result,
            nonce.Length,
            cipherBytes.Length
        );


        Buffer.BlockCopy(
            tag,
            0,
            result,
            nonce.Length +
            cipherBytes.Length,
            tag.Length
        );


        return Convert.ToBase64String(
            result
        );
    }


    // =====================================================
    // DECRYPT TOTP SECRET
    // =====================================================
    //
    // INPUT:
    // Encrypted database value
    //
    // OUTPUT:
    // Original Base32 TOTP secret
    //
    public string Decrypt(
        string encryptedValue)
    {
        var data =
            Convert.FromBase64String(
                encryptedValue
            );


        if (data.Length < 29)
        {
            throw new InvalidOperationException(
                "Invalid encrypted TOTP secret."
            );
        }


        var nonce =
            data[..12];


        var tag =
            data[^16..];


        var cipher =
            data[12..^16];


        var plainBytes =
            new byte[
                cipher.Length
            ];


        using var aes =
            new AesGcm(
                _key,
                16
            );


        aes.Decrypt(
            nonce,
            cipher,
            tag,
            plainBytes
        );


        return Encoding.UTF8.GetString(
            plainBytes
        );
    }
}