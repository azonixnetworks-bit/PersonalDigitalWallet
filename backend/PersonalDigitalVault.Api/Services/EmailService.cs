using System.Net;
using System.Net.Mail;

using PersonalDigitalVault.Api.Interfaces.Services;


namespace PersonalDigitalVault.Api.Services;


public class EmailService
    : IEmailService
{
    // =====================================================
    // CONFIGURATION
    // =====================================================
    //
    // INPUT:
    // appsettings + User Secrets
    //
    // REASON:
    // SMTP username/password and application URL
    // configuration-la irundhu read panna.
    //
    private readonly IConfiguration
        _configuration;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public EmailService(
        IConfiguration configuration)
    {
        _configuration =
            configuration;
    }


    // =====================================================
    // SMTP SETTINGS READ
    // =====================================================
    //
    // REASON:
    // OTP email
    // Share email
    // Password reset email
    //
    // moonu methods-layum same SMTP configuration
    // duplicate validation pannaama use panna.
    //
    private (
        string Host,
        int Port,
        string Username,
        string Password,
        string FromEmail,
        string FromName)
        GetEmailSettings()
    {
        var host =
            _configuration[
                "Email:SmtpHost"
            ];


        var portText =
            _configuration[
                "Email:SmtpPort"
            ];


        var username =
            _configuration[
                "Email:Username"
            ];


        var password =
            _configuration[
                "Email:Password"
            ];


        var fromEmail =
            _configuration[
                "Email:FromEmail"
            ];


        var fromName =
            _configuration[
                "Email:FromName"
            ];


        // =================================================
        // REQUIRED CONFIG CHECK
        // =================================================

        if (
            string.IsNullOrWhiteSpace(host)
            ||
            string.IsNullOrWhiteSpace(portText)
            ||
            string.IsNullOrWhiteSpace(username)
            ||
            string.IsNullOrWhiteSpace(password)
            ||
            string.IsNullOrWhiteSpace(fromEmail)
        )
        {
            throw new InvalidOperationException(
                "Email configuration is incomplete."
            );
        }


        // =================================================
        // PORT VALIDATION
        // =================================================
        //
        // int.Parse use pannina invalid config-la
        // generic FormatException varum.
        //
        // TryParse use panni safe config error kuduppom.
        //
        if (
            !int.TryParse(
                portText,
                out var port
            )
            ||
            port <= 0
            ||
            port > 65535
        )
        {
            throw new InvalidOperationException(
                "Email SMTP port is invalid."
            );
        }


        return (
            host.Trim(),
            port,
            username.Trim(),
            password,
            fromEmail.Trim(),
            string.IsNullOrWhiteSpace(fromName)
                ? "Personal Digital Vault"
                : fromName.Trim()
        );
    }


    // =====================================================
    // CREATE SMTP CLIENT
    // =====================================================
    //
    // OUTPUT:
    // Configured SMTP client
    //
    private SmtpClient CreateSmtpClient(
        string host,
        int port,
        string username,
        string password)
    {
        var smtp =
            new SmtpClient(
                host,
                port
            );


        // TLS / SSL connection enable.
        smtp.EnableSsl =
            true;


        // Windows/default credential
        // accidentally use panna koodathu.
        smtp.UseDefaultCredentials =
            false;


        smtp.Credentials =
            new NetworkCredential(
                username,
                password
            );


        return smtp;
    }


    // =====================================================
    // SEND EMAIL VERIFICATION OTP
    // =====================================================
    //
    // INPUT:
    // email
    // fullName
    // otp
    //
    // REASON:
    // Registration email ownership verify panna.
    //
    // OUTPUT:
    // Email send complete.
    //
    public async Task SendVerificationOtpAsync(
        string email,
        string fullName,
        string otp)
    {
        // =================================================
        // BASIC INPUT CHECK
        // =================================================

        if (
            string.IsNullOrWhiteSpace(email)
            ||
            string.IsNullOrWhiteSpace(fullName)
            ||
            string.IsNullOrWhiteSpace(otp)
        )
        {
            throw new ArgumentException(
                "Email verification message data is incomplete."
            );
        }


        // =================================================
        // SMTP SETTINGS
        // =================================================

        var settings =
            GetEmailSettings();


        using var smtp =
            CreateSmtpClient(
                settings.Host,
                settings.Port,
                settings.Username,
                settings.Password
            );


        // =================================================
        // EMAIL MESSAGE
        // =================================================

        using var message =
            new MailMessage();


        message.From =
            new MailAddress(
                settings.FromEmail,
                settings.FromName
            );


        message.To.Add(
            email.Trim()
        );


        message.Subject =
            "Personal Digital Vault - Email Verification Code";


        message.IsBodyHtml =
            true;


        // =================================================
        // SAFE HTML VALUES
        // =================================================
        //
        // User-controlled name direct HTML-la
        // insert panna koodathu.
        //
        var safeFullName =
            WebUtility.HtmlEncode(
                fullName
            );


        var safeOtp =
            WebUtility.HtmlEncode(
                otp
            );


        // =================================================
        // EMAIL BODY
        // =================================================

        message.Body = $@"
<html>
<body style='
    font-family:Arial,sans-serif;
    background:#f5f7fb;
    padding:30px;
'>

    <div style='
        max-width:600px;
        margin:auto;
        background:white;
        padding:30px;
        border-radius:12px;
    '>

        <h2>
            Personal Digital Vault
        </h2>

        <p>
            Hi {safeFullName},
        </p>

        <p>
            Your email verification code is:
        </p>

        <div style='
            font-size:30px;
            font-weight:bold;
            letter-spacing:8px;
            margin:20px 0;
        '>
            {safeOtp}
        </div>

        <p>
            This code expires in
            <strong>
                5 minutes
            </strong>.
        </p>

        <p style='
            font-size:13px;
            color:#64748b;
        '>
            If you did not request this code,
            you can ignore this email.
        </p>

    </div>

</body>
</html>
";


        // =================================================
        // SEND
        // =================================================

        await smtp.SendMailAsync(
            message
        );
    }


    // =====================================================
    // SEND DOCUMENT SHARE INVITATION
    // =====================================================
    //
    // INPUT:
    //
    // recipientEmail
    // recipientName
    // ownerName
    // fileName
    // invitationToken
    //
    // SECURITY:
    //
    // Raw invitation token:
    //
    //      Email link-la mattum.
    //
    // Database:
    //
    //      SHA-256 token hash mattum.
    //
    // OUTPUT:
    //
    // Secure email invitation.
    //
    public async Task
        SendDocumentShareInvitationAsync(
            string recipientEmail,
            string recipientName,
            string ownerName,
            string fileName,
            string invitationToken)
    {
        // =================================================
        // INPUT VALIDATION
        // =================================================

        if (
            string.IsNullOrWhiteSpace(recipientEmail)
            ||
            string.IsNullOrWhiteSpace(recipientName)
            ||
            string.IsNullOrWhiteSpace(ownerName)
            ||
            string.IsNullOrWhiteSpace(fileName)
            ||
            string.IsNullOrWhiteSpace(invitationToken)
        )
        {
            throw new ArgumentException(
                "Document share invitation data is incomplete."
            );
        }


        // =================================================
        // SMTP SETTINGS
        // =================================================

        var settings =
            GetEmailSettings();


        // =================================================
        // APPLICATION BASE URL
        // =================================================

        var baseUrl =
            _configuration[
                "App:FrontendBaseUrl"
            ];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl =
                _configuration[
                    "App:BaseUrl"
                ];
        }


        if (
            string.IsNullOrWhiteSpace(
                baseUrl
            )
        )
        {
            throw new InvalidOperationException(
                "App:BaseUrl is missing."
            );
        }


        // =================================================
        // APPLICATION URL VALIDATION
        // =================================================

        if (
            !Uri.TryCreate(
                baseUrl,
                UriKind.Absolute,
                out var parsedBaseUrl
            )
        )
        {
            throw new InvalidOperationException(
                "App:BaseUrl is invalid."
            );
        }


        if (
            parsedBaseUrl.Scheme !=
                Uri.UriSchemeHttps
            &&
            !string.Equals(
                parsedBaseUrl.Host,
                "localhost",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new InvalidOperationException(
                "App:BaseUrl must use HTTPS."
            );
        }


        // =================================================
        // SECURE TOKEN ENCODING
        // =================================================

        var safeToken =
            Uri.EscapeDataString(
                invitationToken.Trim()
            );


        // =================================================
        // SECURE CLIENT-SIDE TOKEN FRAGMENT
        // =================================================
        //
        // WRONG:
        //
        // share-invitation.html?token=SECRET
        //
        // Query string server request/logs-la
        // appear aagalam.
        //
        //
        // CORRECT:
        //
        // share-invitation.html#token=SECRET
        //
        // URL fragment HTTP request server-ku
        // send aagadhu.
        //
        var invitationUrl =
            $"{baseUrl.TrimEnd('/')}" +
            "/share-invitation" +
            $"#token={safeToken}";


        // =================================================
        // SMTP CLIENT
        // =================================================

        using var smtp =
            CreateSmtpClient(
                settings.Host,
                settings.Port,
                settings.Username,
                settings.Password
            );


        // =================================================
        // EMAIL MESSAGE
        // =================================================

        using var message =
            new MailMessage();


        message.From =
            new MailAddress(
                settings.FromEmail,
                settings.FromName
            );


        message.To.Add(
            recipientEmail.Trim()
        );


        message.Subject =
            "A secure document has been shared with you";


        message.IsBodyHtml =
            true;


        // =================================================
        // HTML ENCODING
        // =================================================
        //
        // Recipient name
        // Owner name
        // File name
        // URL
        //
        // direct HTML injection avoid panna.
        //
        var safeRecipientName =
            WebUtility.HtmlEncode(
                recipientName
            );


        var safeOwnerName =
            WebUtility.HtmlEncode(
                ownerName
            );


        var safeFileName =
            WebUtility.HtmlEncode(
                fileName
            );


        var safeInvitationUrl =
            WebUtility.HtmlEncode(
                invitationUrl
            );


        // =================================================
        // EMAIL BODY
        // =================================================

        message.Body = $@"
<html>
<body style='
    margin:0;
    padding:30px;
    background:#f4f7fb;
    font-family:Arial,sans-serif;
    color:#172033;
'>

    <div style='
        max-width:620px;
        margin:auto;
        background:#ffffff;
        border-radius:14px;
        padding:32px;
    '>

        <h2 style='
            color:#0f2747;
        '>
            Personal Digital Vault
        </h2>


        <p>
            Hi {safeRecipientName},
        </p>


        <p>
            <strong>
                {safeOwnerName}
            </strong>

            has securely shared a
            document with you.
        </p>


        <div style='
            background:#f5f7fa;
            padding:18px;
            border-radius:10px;
            margin:20px 0;
        '>

            <strong>
                Document
            </strong>

            <br>

            {safeFileName}

        </div>


        <p>
            For security, you must sign in
            to your Personal Digital Vault
            account and complete two-factor
            authentication before accessing
            this document.
        </p>


        <p style='
            margin:28px 0;
        '>

            <a
                href='{safeInvitationUrl}'

                style='
                    background:#2563eb;
                    color:white;
                    padding:13px 20px;
                    text-decoration:none;
                    border-radius:8px;
                    display:inline-block;
                '>

                View Secure Document

            </a>

        </p>


        <p style='
            font-size:13px;
            color:#64748b;
        '>

            This invitation link expires
            in 24 hours.

        </p>


        <p style='
            font-size:13px;
            color:#64748b;
        '>

            The link alone does not grant access.
            You must sign in using the account
            that this document was shared with.

        </p>


        <p style='
            font-size:13px;
            color:#64748b;
        '>

            After successful acceptance,
            this invitation link cannot be reused.

        </p>

    </div>

</body>
</html>
";


        // =================================================
        // SEND EMAIL
        // =================================================

        await smtp.SendMailAsync(
            message
        );
    }


    // =====================================================
    // SEND PASSWORD RESET EMAIL
    // =====================================================
    //
    // Function:
    //
    // Forgot Password request success aana
    // registered user-ku secure password reset
    // link send pannum.
    //
    //
    // INPUT:
    //
    // email
    //      -> reset request pannina user email.
    //
    // fullName
    //      -> email greeting-ku use pannuvom.
    //
    // resetToken
    //      -> PasswordResetTokenService
    //         generate panna random token.
    //
    //
    // SECURITY:
    //
    // Raw reset token:
    //
    //      Email link-la mattum irukkum.
    //
    // Database:
    //
    //      SHA-256 hash mattum irukkum.
    //
    //
    // OUTPUT:
    //
    // User-ku password reset email send aagum.
    //
    public async Task SendPasswordResetEmailAsync(
        string email,
        string fullName,
        string resetToken)
    {
        // =================================================
        // INPUT VALIDATION
        // =================================================
        //
        // Empty data irundha invalid email
        // generate panna koodathu.
        //
        if (
            string.IsNullOrWhiteSpace(email)
            ||
            string.IsNullOrWhiteSpace(fullName)
            ||
            string.IsNullOrWhiteSpace(resetToken)
        )
        {
            throw new ArgumentException(
                "Password reset email data is incomplete."
            );
        }


        // =================================================
        // SMTP SETTINGS
        // =================================================
        //
        // Existing Email configuration use pannuvom.
        //
        var settings =
            GetEmailSettings();


        // =================================================
        // APPLICATION BASE URL
        // =================================================
        //
        // Example:
        //
        // https://localhost:7240
        //
        // Reset URL request Host header-la irundhu
        // dynamically build panna maatom.
        //
        // Trusted application configuration
        // App:BaseUrl use pannuvom.
        //
        var baseUrl =
            _configuration[
                "App:BaseUrl"
            ];


        if (
            string.IsNullOrWhiteSpace(
                baseUrl
            )
        )
        {
            throw new InvalidOperationException(
                "App:BaseUrl is missing."
            );
        }


        // =================================================
        // BASE URL FORMAT VALIDATION
        // =================================================

        if (
            !Uri.TryCreate(
                baseUrl,
                UriKind.Absolute,
                out var parsedBaseUrl
            )
        )
        {
            throw new InvalidOperationException(
                "App:BaseUrl is invalid."
            );
        }


        // =================================================
        // HTTPS VALIDATION
        // =================================================
        //
        // Production:
        // HTTPS mandatory.
        //
        // Local development:
        // localhost allow pannrom.
        //
        if (
            parsedBaseUrl.Scheme !=
                Uri.UriSchemeHttps
            &&
            !string.Equals(
                parsedBaseUrl.Host,
                "localhost",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new InvalidOperationException(
                "App:BaseUrl must use HTTPS."
            );
        }


        // =================================================
        // TOKEN URL ENCODING
        // =================================================
        //
        // Token URL-la safe-aa use panna
        // encode pannuvom.
        //
        var safeToken =
            Uri.EscapeDataString(
                resetToken.Trim()
            );


        // =================================================
        // PASSWORD RESET URL
        // =================================================
        //
        // Example:
        //
        // https://localhost:7240/
        // html/reset-password.html
        // #token=ABC...
        //
        //
        // Important:
        //
        // Token query string-la podala.
        //
        // ?token=ABC
        //
        // use pannaama:
        //
        // #token=ABC
        //
        // fragment use pannrom.
        //
        // Browser JavaScript later:
        // window.location.hash
        //
        // use panni token read pannum.
        //
        var resetUrl =
            $"{baseUrl.TrimEnd('/')}" +
            "/html/reset-password.html" +
            $"#token={safeToken}";


        // =================================================
        // SMTP CLIENT
        // =================================================

        using var smtp =
            CreateSmtpClient(
                settings.Host,
                settings.Port,
                settings.Username,
                settings.Password
            );


        // =================================================
        // EMAIL MESSAGE
        // =================================================

        using var message =
            new MailMessage();


        message.From =
            new MailAddress(
                settings.FromEmail,
                settings.FromName
            );


        message.To.Add(
            email.Trim()
        );


        message.Subject =
            "Personal Digital Vault - Reset Your Password";


        message.IsBodyHtml =
            true;


        // =================================================
        // HTML ENCODING
        // =================================================
        //
        // User-controlled values HTML-la direct
        // insert panna koodathu.
        //
        var safeFullName =
            WebUtility.HtmlEncode(
                fullName
            );


        var safeResetUrl =
            WebUtility.HtmlEncode(
                resetUrl
            );


        // =================================================
        // PASSWORD RESET EMAIL BODY
        // =================================================

        message.Body = $@"
<html>

<body style='
    margin:0;
    padding:30px;
    background:#f4f7fb;
    font-family:Arial,sans-serif;
    color:#172033;
'>

    <div style='
        max-width:620px;
        margin:auto;
        background:#ffffff;
        border-radius:14px;
        padding:32px;
    '>

        <h2 style='
            margin-top:0;
            color:#0f2747;
        '>
            Personal Digital Vault
        </h2>


        <p>
            Hi {safeFullName},
        </p>


        <p>
            We received a request to reset
            the password for your
            Personal Digital Vault account.
        </p>


        <p>
            Click the button below to
            create a new password.
        </p>


        <p style='
            margin:28px 0;
        '>

            <a
                href='{safeResetUrl}'

                style='
                    background:#2563eb;
                    color:#ffffff;
                    padding:13px 22px;
                    text-decoration:none;
                    border-radius:8px;
                    display:inline-block;
                    font-weight:bold;
                '>

                Reset Password

            </a>

        </p>


        <div style='
            background:#f8fafc;
            border:1px solid #e2e8f0;
            border-radius:10px;
            padding:16px;
            margin:20px 0;
        '>

            <strong>
                Security Notice
            </strong>

            <p style='
                margin-bottom:0;
            '>
                This password reset link
                expires in 15 minutes and
                can only be used once.
            </p>

        </div>


        <p style='
            font-size:13px;
            color:#64748b;
        '>

            If you did not request a
            password reset, you can safely
            ignore this email.

        </p>


        <p style='
            font-size:13px;
            color:#64748b;
        '>

            Your existing password will
            remain unchanged until a valid
            reset request is completed.

        </p>


        <p style='
            font-size:13px;
            color:#64748b;
        '>

            For your security, never share
            this password reset link with
            anyone.

        </p>

    </div>

</body>

</html>
";


        // =================================================
        // SEND PASSWORD RESET EMAIL
        // =================================================
        //
        // SendMailAsync asynchronous-aa SMTP
        // server-ku email send pannum.
        //
        await smtp.SendMailAsync(
            message
        );
    }
}