using System.ComponentModel.DataAnnotations;

namespace PersonalDigitalVault.Api.DTOs.Auth;

public class ForgotPasswordRequestDto
{
    // =========================================================
    // EMAIL
    // =========================================================
    //
    // Function:
    // Password marantha user email address receive pannum.
    //
    // Input:
    // user@example.com
    //
    // Reason:
    // Endha account-ku password reset request nu
    // backend identify panna.
    //
    // Security:
    // API response account exist-a illaya nu
    // reveal panna koodathu.
    //
    // Output:
    // AuthService-ku validated email pass aagum.

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; }
        = string.Empty;
}