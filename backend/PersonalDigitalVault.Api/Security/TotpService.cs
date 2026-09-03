using OtpNet;
using QRCoder;

namespace PersonalDigitalVault.Api.Security;

public class TotpService
{
    // =========================================================
    // AUTHENTICATOR ISSUER
    // =========================================================

    // Google Authenticator-la account name pakkathula
    // application identify panna use aagum.
    private const string Issuer =
        "Personal Digital Vault";


    // =========================================================
    // TOTP SETTINGS
    // =========================================================

    // Google Authenticator standard time period.
    private const int TotpPeriodSeconds =
        30;


    // TOTP output 6 digits.
    private const int TotpDigits =
        6;


    // 20 bytes = 160-bit random TOTP secret.
    private const int SecretSizeBytes =
        20;


    // =========================================================
    // TOTP SECRET GENERATE
    // =========================================================

    // Function:
    // New random Authenticator secret generate pannum.
    //
    // Input:
    // None.
    //
    // Reason:
    // Ovvoru user-kum unique TOTP secret thevai.
    //
    // Output:
    // Base32 encoded secret.
    //
    // Example:
    // JBSWY3DPEHPK3PXP...
    //
    // Security:
    // Cryptographically generated random key use pannrom.
    public string GenerateSecret()
    {
        byte[] secretBytes =
            KeyGeneration.GenerateRandomKey(
                SecretSizeBytes);


        return Base32Encoding.ToString(
            secretBytes);
    }


    // =========================================================
    // OTPAUTH URI GENERATE
    // =========================================================

    // Function:
    // Google Authenticator understand pannura
    // otpauth:// URI generate pannum.
    //
    // Input:
    // secret = Base32 TOTP secret
    // email  = user email
    //
    // Output:
    // otpauth://totp/... URI.
    //
    // Security:
    // Secret sensitive.
    // URI logs/database-la unnecessary-aa save panna koodathu.
    public string GenerateOtpAuthUri(
        string secret,
        string email)
    {
        if (string.IsNullOrWhiteSpace(
                secret))
        {
            throw new ArgumentException(
                "TOTP secret is required.",
                nameof(secret));
        }


        if (string.IsNullOrWhiteSpace(
                email))
        {
            throw new ArgumentException(
                "Email is required.",
                nameof(email));
        }


        string normalizedEmail =
            email
                .Trim()
                .ToLowerInvariant();


        string label =
            Uri.EscapeDataString(
                $"{Issuer}:{normalizedEmail}");


        string encodedIssuer =
            Uri.EscapeDataString(
                Issuer);


        return
            $"otpauth://totp/{label}" +
            $"?secret={secret}" +
            $"&issuer={encodedIssuer}" +
            $"&digits={TotpDigits}" +
            $"&period={TotpPeriodSeconds}";
    }


    // =========================================================
    // QR CODE GENERATE
    // =========================================================

    // Function:
    // OTPAuth URI-a PNG QR image-a convert pannum.
    //
    // Input:
    // otpAuthUri
    //
    // Output:
    // data:image/png;base64,...
    //
    // Frontend:
    //
    // <img src="...">
    //
    // Security:
    // QR code TOTP secret contain pannum.
    // Setup page time mattum user-ku show panna vendum.
    public string GenerateQrCodeDataUrl(
        string otpAuthUri)
    {
        if (string.IsNullOrWhiteSpace(
                otpAuthUri))
        {
            throw new ArgumentException(
                "OTP Auth URI is required.",
                nameof(otpAuthUri));
        }


        using var generator =
            new QRCodeGenerator();


        using var qrData =
            generator.CreateQrCode(
                otpAuthUri,
                QRCodeGenerator.ECCLevel.Q);


        using var qrCode =
            new PngByteQRCode(
                qrData);


        byte[] qrBytes =
            qrCode.GetGraphic(
                20);


        return
            "data:image/png;base64," +
            Convert.ToBase64String(
                qrBytes);
    }


    // =========================================================
    // VERIFY TOTP - EXISTING API
    // =========================================================

    // Function:
    // Existing project calls break aagama
    // simple true/false verification provide pannum.
    //
    // Input:
    // secret
    // code
    //
    // Output:
    // true / false
    //
    // IMPORTANT:
    // Indha overload matched time step-a discard pannum.
    //
    // Existing AuthService compile compatibility-ku
    // temporarily preserve pannrom.
    public bool VerifyCode(
        string secret,
        string code)
    {
        return VerifyCode(
            secret,
            code,
            out _);
    }


    // =========================================================
    // VERIFY TOTP + MATCHED TIME STEP
    // =========================================================

    // Function:
    // Authenticator code verify pannitu
    // match aana TOTP time-step-um return pannum.
    //
    // Input:
    // secret
    // code
    //
    // Output:
    // true / false
    //
    // out:
    // matchedTimeStep
    //
    // Reason:
    // Later replay prevention-ku:
    //
    // same matched time-step already use aachaa?
    //
    // nu database-la check panna.
    //
    // SECURITY:
    // This method alone replay prevent pannaathu.
    // Caller matchedTimeStep persist/check panna vendum.
    public bool VerifyCode(
        string secret,
        string code,
        out long matchedTimeStep)
    {
        matchedTimeStep =
            0;


        // =====================================================
        // BASIC INPUT CHECK
        // =====================================================

        if (string.IsNullOrWhiteSpace(
                secret) ||
            string.IsNullOrWhiteSpace(
                code))
        {
            return false;
        }


        string cleanCode =
            code.Trim();


        // Exactly 6 characters.
        if (cleanCode.Length !=
            TotpDigits)
        {
            return false;
        }


        // ASCII 0-9 digits mattum allow.
        //
        // char.IsDigit Unicode digits-um accept pannalaam.
        // Authenticator code-ku ASCII numbers mattum expect pannrom.
        foreach (char character in cleanCode)
        {
            if (character < '0' ||
                character > '9')
            {
                return false;
            }
        }


        // =====================================================
        // DECODE BASE32 SECRET
        // =====================================================

        byte[] secretBytes;


        try
        {
            secretBytes =
                Base32Encoding.ToBytes(
                    secret.Trim());
        }
        catch
        {
            // Database secret invalid/corrupted-na
            // authentication request 500 aaga koodathu.
            return false;
        }


        if (secretBytes.Length == 0)
        {
            return false;
        }


        // =====================================================
        // CREATE TOTP
        // =====================================================

        var totp =
            new Totp(
                secretBytes,

                // 30-second period.
                step:
                    TotpPeriodSeconds,

                // Google Authenticator compatible default.
                mode:
                    OtpHashMode.Sha1,

                // 6-digit output.
                totpSize:
                    TotpDigits);


        // =====================================================
        // VERIFY
        // =====================================================

        // Otp.NET returns which time-step matched.
        //
        // RfcSpecifiedNetworkDelay gives small tolerance
        // for client/server clock difference.
        bool valid =
            totp.VerifyTotp(
                cleanCode,
                out long timeStepUsed,
                VerificationWindow
                    .RfcSpecifiedNetworkDelay);


        if (!valid)
        {
            return false;
        }


        matchedTimeStep =
            timeStepUsed;


        return true;
    }
}