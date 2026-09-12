using System.Security.Cryptography;
using System.Text;

namespace PersonalDigitalVault.Api.Security;

public sealed class GoogleOAuthStateService
{
    private readonly byte[]? _signingKey;

    public GoogleOAuthStateService(IConfiguration configuration)
    {
        string? key = configuration["GoogleDrive:StateSigningKey"];

        if (!string.IsNullOrWhiteSpace(key) && Encoding.UTF8.GetByteCount(key) >= 32)
        {
            _signingKey = Encoding.UTF8.GetBytes(key);
        }
    }

    public bool IsConfigured => _signingKey is not null;

    public string Create(int userId, TimeSpan lifetime)
    {
        byte[] signingKey = GetSigningKey();

        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        long expires = DateTimeOffset.UtcNow.Add(lifetime).ToUnixTimeSeconds();
        string nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        string payload = $"{userId}|{expires}|{nonce}";
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        byte[] signature = HMACSHA256.HashData(signingKey, payloadBytes);

        return $"{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(signature)}";
    }

    public int ValidateAndGetUserId(string state)
    {
        byte[] signingKey = GetSigningKey();

        if (string.IsNullOrWhiteSpace(state))
        {
            throw new InvalidOperationException("Invalid OAuth state.");
        }

        string[] parts = state.Split('.', 2);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException("Invalid OAuth state.");
        }

        byte[] payloadBytes;
        byte[] suppliedSignature;

        try
        {
            payloadBytes = Base64UrlDecode(parts[0]);
            suppliedSignature = Base64UrlDecode(parts[1]);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Invalid OAuth state.");
        }

        byte[] expectedSignature = HMACSHA256.HashData(signingKey, payloadBytes);
        if (suppliedSignature.Length != expectedSignature.Length ||
            !CryptographicOperations.FixedTimeEquals(suppliedSignature, expectedSignature))
        {
            throw new InvalidOperationException("Invalid OAuth state.");
        }

        string[] payload = Encoding.UTF8.GetString(payloadBytes).Split('|');
        if (payload.Length != 3 ||
            !int.TryParse(payload[0], out int userId) ||
            userId <= 0 ||
            !long.TryParse(payload[1], out long expires))
        {
            throw new InvalidOperationException("Invalid OAuth state.");
        }

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expires)
        {
            throw new InvalidOperationException("OAuth state has expired.");
        }

        return userId;
    }

    private byte[] GetSigningKey()
    {
        return _signingKey ?? throw new InvalidOperationException(
            "Google Drive backup is not configured. GoogleDrive:StateSigningKey must contain at least 32 bytes.");
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        string padded = value.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            0 => string.Empty,
            _ => throw new FormatException("Invalid Base64Url value.")
        };

        return Convert.FromBase64String(padded);
    }
}
