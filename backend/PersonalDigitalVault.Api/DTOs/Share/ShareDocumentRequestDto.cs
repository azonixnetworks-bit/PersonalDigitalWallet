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
    public string RecipientEmail { get; set; }
        = string.Empty;
}