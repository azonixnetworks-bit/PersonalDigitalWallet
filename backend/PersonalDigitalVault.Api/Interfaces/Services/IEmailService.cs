namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface IEmailService
{
    // =====================================================
    // EMAIL VERIFICATION OTP
    // =====================================================
    //
    // Function:
    // Registration time-la email verification OTP
    // user-ku send pannum.
    //
    // Input:
    // email
    // fullName
    // otp
    //
    // Output:
    // Verification email send aagum.
    //
    Task SendVerificationOtpAsync(
        string email,
        string fullName,
        string otp);


    // =====================================================
    // DOCUMENT SHARE INVITATION
    // =====================================================
    //
    // Function:
    // Secure document share invitation email
    // recipient-ku send pannum.
    //
    // INPUT:
    // recipientEmail
    // recipientName
    // ownerName
    // fileName
    // invitationToken
    //
    // OUTPUT:
    // Recipient-ku secure email invitation send aagum.
    //
    Task SendDocumentShareInvitationAsync(
        string recipientEmail,
        string recipientName,
        string ownerName,
        string fileName,
        string invitationToken);


    // =====================================================
    // PASSWORD RESET EMAIL
    // =====================================================
    //
    // Function:
    // Forgot Password request-ku secure reset link
    // email-la send pannum.
    //
    // INPUT:
    // email
    // fullName
    // resetToken
    //
    // Security:
    // resetToken raw value email link-la mattum pogum.
    //
    // Database-la raw resetToken store panna maatom.
    // SHA-256 hash mattum store pannuvom.
    //
    // OUTPUT:
    // User email-ku password reset link send aagum.
    //
    Task SendPasswordResetEmailAsync(
        string email,
        string fullName,
        string resetToken);
}