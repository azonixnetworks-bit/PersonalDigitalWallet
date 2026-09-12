using System.ComponentModel.DataAnnotations;

namespace PersonalDigitalVault.Api.DTOs.Share;

public class ShareDocumentRequestDto
{
    // =====================================================
    // RECIPIENT EMAIL
    // =====================================================
    //
    // Owner endha registered user-ku
    // file share panna poraaro
    // avaroda email.
    //
    // Example:
    // alex@gmail.com
    //
    [Required]
    [EmailAddress]
    [StringLength(
        320,
        ErrorMessage =
            "Recipient email cannot exceed 320 characters.")]
    public string RecipientEmail { get; set; }
        = string.Empty;
}