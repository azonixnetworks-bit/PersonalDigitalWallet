using System.Security.Cryptography;
using System.Text;

namespace PersonalDigitalVault.Api.Security;

public class AesEncryption
{
    // =========================================================
    // AES KEY
    // =========================================================

    // User Secrets-la irukkura 32-byte AES key inga store aagum.
    // Input  : Security:AesKeyBase64
    // Reason : Documents + Credentials encrypt/decrypt panna.
    // Output : Internal byte[] key.
    // Security:
    // Key code-kulla hard-code panna koodathu.
    private readonly byte[] _key;


    // =========================================================
    // AES-GCM SETTINGS
    // =========================================================

    // AES-GCM nonce size.
    // Ovvoru encryption-kum new random nonce create pannuvom.
    private const int NonceSize = 12;

    // Authentication tag size.
    // Ciphertext tamper/corrupt aana detect panna use aagum.
    private const int TagSize = 16;

    // Old AES-CBC files use 16-byte IV.
    private const int LegacyIvSize = 16;


    // =========================================================
    // NEW ENCRYPTION FORMAT HEADER
    // =========================================================

    // New AES-GCM encrypted data identify panna header.
    //
    // New format:
    //
    // PDV-GCM1
    // +
    // Nonce
    // +
    // Authentication Tag
    // +
    // CipherText
    //
    // Old data-la indha header irukkathu.
    //
    // Reason:
    // Existing AES-CBC data break aagama
    // new AES-GCM data distinguish panna.
    private static readonly byte[] FormatHeader =
        Encoding.ASCII.GetBytes("PDV-GCM1");


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Configuration-la irundhu AES key read pannum.
    //
    // Input:
    // IConfiguration config
    //
    // Reason:
    // Encryption key User Secrets-la securely store pannirukkom.
    //
    // Output:
    // Valid 32-byte AES key _key variable-la save aagum.
    //
    // Security:
    // Missing / invalid / wrong-size key use panna application
    // startup-la fail aaganum. Weak/random wrong key use panna koodathu.
    public AesEncryption(IConfiguration config)
    {
        string? keyText = config["Security:AesKeyBase64"];

        if (string.IsNullOrWhiteSpace(keyText))
        {
            throw new InvalidOperationException(
                "AES key missing.");
        }

        try
        {
            _key = Convert.FromBase64String(keyText);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "AES key is not valid Base64.");
        }

        // 32 bytes = AES-256 key.
        if (_key.Length != 32)
        {
            throw new InvalidOperationException(
                "AES key must be 32 bytes.");
        }
    }


    // =========================================================
    // ENCRYPT STRING
    // =========================================================

    // Function:
    // String value-a AES-GCM use panni encrypt pannum.
    //
    // Input:
    // value = Username / Password / Notes maari string.
    //
    // Reason:
    // Sensitive credential data plain text-la DB-la save panna koodathu.
    //
    // Output:
    // Base64 encrypted string.
    //
    // Security:
    // Actual encryption EncryptBytes() method-la AES-GCM use pannum.
    public string EncryptString(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        byte[] plainBytes = Encoding.UTF8.GetBytes(value);

        byte[] encryptedBytes = EncryptBytes(plainBytes);

        return Convert.ToBase64String(encryptedBytes);
    }


    // =========================================================
    // DECRYPT STRING
    // =========================================================

    // Function:
    // Encrypted Base64 string-a decrypt pannum.
    //
    // Input:
    // AES encrypted Base64 string.
    //
    // Reason:
    // Authorized user credential reveal panna original value venum.
    //
    // Output:
    // Original plaintext string.
    //
    // Security:
    // Invalid/corrupt encrypted value irundha safe crypto error throw pannum.
    public string DecryptString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CryptographicException(
                "Encrypted data is invalid or corrupted.");
        }

        try
        {
            byte[] encryptedBytes =
                Convert.FromBase64String(value);

            byte[] plainBytes =
                DecryptBytes(encryptedBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (FormatException)
        {
            // Invalid Base64 value.
            throw new CryptographicException(
                "Encrypted data is invalid or corrupted.");
        }
    }


    // =========================================================
    // ENCRYPT BYTES - NEW AES-GCM
    // =========================================================

    // Function:
    // Document/file bytes-a AES-GCM use panni encrypt pannum.
    //
    // Input:
    // plain = original file bytes.
    //
    // Reason:
    // File content SecureStorage-la plaintext-a store panna koodathu.
    //
    // Output:
    //
    // PDV-GCM1
    // +
    // Nonce
    // +
    // Tag
    // +
    // CipherText
    //
    // Security:
    // AES-GCM confidentiality + authentication provide pannum.
    // Ciphertext modify aana tag validation fail aagum.
    public byte[] EncryptBytes(byte[] plain)
    {
        if (plain == null)
        {
            throw new ArgumentNullException(nameof(plain));
        }

        // Ovvoru encryption-kum fresh random nonce.
        byte[] nonce =
            RandomNumberGenerator.GetBytes(NonceSize);

        // Ciphertext plaintext same length.
        byte[] cipher =
            new byte[plain.Length];

        // Authentication tag.
        byte[] tag =
            new byte[TagSize];

        // Current recommended constructor-la
        // required tag size explicit-a kudukkirom.
        using (var aesGcm = new AesGcm(_key, TagSize))
        {
            aesGcm.Encrypt(
                nonce,
                plain,
                cipher,
                tag);
        }

        // Final encrypted data size calculate pannrom.
        byte[] result =
            new byte[
                FormatHeader.Length +
                NonceSize +
                TagSize +
                cipher.Length
            ];

        int offset = 0;


        // -----------------------------------------------------
        // 1. HEADER
        // -----------------------------------------------------

        Buffer.BlockCopy(
            FormatHeader,
            0,
            result,
            offset,
            FormatHeader.Length);

        offset += FormatHeader.Length;


        // -----------------------------------------------------
        // 2. NONCE
        // -----------------------------------------------------

        Buffer.BlockCopy(
            nonce,
            0,
            result,
            offset,
            NonceSize);

        offset += NonceSize;


        // -----------------------------------------------------
        // 3. AUTHENTICATION TAG
        // -----------------------------------------------------

        Buffer.BlockCopy(
            tag,
            0,
            result,
            offset,
            TagSize);

        offset += TagSize;


        // -----------------------------------------------------
        // 4. CIPHERTEXT
        // -----------------------------------------------------

        Buffer.BlockCopy(
            cipher,
            0,
            result,
            offset,
            cipher.Length);


        return result;
    }


    // =========================================================
    // DECRYPT BYTES
    // =========================================================

    // Function:
    // Encrypted bytes old CBC format-aa
    // illa new GCM format-aa identify pannum.
    //
    // Input:
    // encrypted byte[].
    //
    // Reason:
    // Existing encrypted Documents/Credentials break aagama
    // backward compatibility maintain panna.
    //
    // Output:
    // Original plaintext bytes.
    public byte[] DecryptBytes(byte[] encrypted)
    {
        if (encrypted == null)
        {
            throw new ArgumentNullException(nameof(encrypted));
        }

        // New format header irundha AES-GCM.
        if (IsNewGcmFormat(encrypted))
        {
            return DecryptGcm(encrypted);
        }

        // Header illana old AES-CBC data-nu treat pannuvom.
        return DecryptLegacyCbc(encrypted);
    }


    // =========================================================
    // CHECK NEW FORMAT
    // =========================================================

    // Function:
    // Encrypted data PDV-GCM1 header-oda start aagutha check pannum.
    //
    // Input:
    // encrypted bytes.
    //
    // Reason:
    // Old CBC data vs new AES-GCM data identify panna.
    //
    // Output:
    // true  = new GCM format
    // false = legacy CBC format
    private bool IsNewGcmFormat(byte[] encrypted)
    {
        if (encrypted.Length < FormatHeader.Length)
        {
            return false;
        }

        for (int i = 0; i < FormatHeader.Length; i++)
        {
            if (encrypted[i] != FormatHeader[i])
            {
                return false;
            }
        }

        return true;
    }


    // =========================================================
    // DECRYPT NEW AES-GCM
    // =========================================================

    // Function:
    // New PDV-GCM1 encrypted value decrypt pannum.
    //
    // Input:
    //
    // Header + Nonce + Tag + CipherText
    //
    // Reason:
    // Authenticated encryption provide panna.
    //
    // Output:
    // Original plaintext bytes.
    //
    // Security:
    // Ciphertext / nonce / tag modify aana
    // AES-GCM authentication fail aagum.
    private byte[] DecryptGcm(byte[] encrypted)
    {
        int minimumLength =
            FormatHeader.Length +
            NonceSize +
            TagSize;

        // Header + nonce + tag kooda illa-na
        // data truncated/corrupted.
        if (encrypted.Length < minimumLength)
        {
            throw new CryptographicException(
                "Encrypted data is invalid or corrupted.");
        }

        int offset = FormatHeader.Length;


        // -----------------------------------------------------
        // NONCE READ
        // -----------------------------------------------------

        byte[] nonce =
            new byte[NonceSize];

        Buffer.BlockCopy(
            encrypted,
            offset,
            nonce,
            0,
            NonceSize);

        offset += NonceSize;


        // -----------------------------------------------------
        // TAG READ
        // -----------------------------------------------------

        byte[] tag =
            new byte[TagSize];

        Buffer.BlockCopy(
            encrypted,
            offset,
            tag,
            0,
            TagSize);

        offset += TagSize;


        // -----------------------------------------------------
        // CIPHERTEXT READ
        // -----------------------------------------------------

        int cipherLength =
            encrypted.Length - offset;

        byte[] cipher =
            new byte[cipherLength];

        Buffer.BlockCopy(
            encrypted,
            offset,
            cipher,
            0,
            cipherLength);


        // Plaintext length ciphertext length same.
        byte[] plain =
            new byte[cipherLength];


        try
        {
            using var aesGcm =
                new AesGcm(_key, TagSize);

            aesGcm.Decrypt(
                nonce,
                cipher,
                tag,
                plain);

            return plain;
        }
        catch (CryptographicException)
        {
            // Wrong key / modified tag / modified ciphertext /
            // corrupted encrypted file.
            throw new CryptographicException(
                "Encrypted data is invalid or corrupted.");
        }
    }


    // =========================================================
    // LEGACY AES-CBC DECRYPT
    // =========================================================

    // Function:
    // Old project-la already encrypt pannina
    // IV + CipherText format data decrypt pannum.
    //
    // Input:
    // Old AES-CBC encrypted bytes.
    //
    // Reason:
    // Existing Documents/Credentials immediately break aagama
    // backward compatibility maintain panna.
    //
    // Output:
    // Original plaintext bytes.
    //
    // IMPORTANT:
    // New encryption-ku indha method use panna maatom.
    // Idhu existing old data decrypt panna mattum.
    private byte[] DecryptLegacyCbc(byte[] encrypted)
    {
        // Old format minimum:
        //
        // 16-byte IV
        // +
        // at least one encrypted block.
        if (encrypted.Length <= LegacyIvSize)
        {
            throw new CryptographicException(
                "Encrypted data is invalid or corrupted.");
        }

        int cipherLength =
            encrypted.Length - LegacyIvSize;


        // AES block size = 16 bytes.
        // Old CBC ciphertext full blocks-a irukkanum.
        if (cipherLength % 16 != 0)
        {
            throw new CryptographicException(
                "Encrypted data is invalid or corrupted.");
        }


        byte[] iv =
            new byte[LegacyIvSize];

        byte[] cipher =
            new byte[cipherLength];


        // First 16 bytes = old IV.
        Buffer.BlockCopy(
            encrypted,
            0,
            iv,
            0,
            LegacyIvSize);


        // Remaining bytes = ciphertext.
        Buffer.BlockCopy(
            encrypted,
            LegacyIvSize,
            cipher,
            0,
            cipherLength);


        try
        {
            using var aes = Aes.Create();

            aes.Key = _key;
            aes.IV = iv;

            // Old project behavior explicit-a define pannrom.
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;


            using var decryptor =
                aes.CreateDecryptor();


            return decryptor.TransformFinalBlock(
                cipher,
                0,
                cipher.Length);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException(
                "Encrypted data is invalid or corrupted.");
        }
    }
}