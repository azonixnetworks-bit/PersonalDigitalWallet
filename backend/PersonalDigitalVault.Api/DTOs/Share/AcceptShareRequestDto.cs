using System.ComponentModel.DataAnnotations;

namespace PersonalDigitalVault.Api.DTOs.Share;

public class AcceptShareRequestDto
{
    // =====================================================
    // INVITATION TOKEN
    // =====================================================
    //
    // Recipient email link-la varra
    // secure random token.
    //
    // Backend indha token-a hash panni
    // database hash-oda compare pannum.
    //
    [Required]
    public string Token { get; set; }
        = string.Empty;
}