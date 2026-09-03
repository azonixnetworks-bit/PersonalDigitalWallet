using System.ComponentModel.DataAnnotations;

namespace PersonalDigitalVault.Api.DTOs.Auth;

public class ResetPasswordRequestDto
{
    // =========================================================
    // RESET TOKEN
    // =========================================================
    //
    // Function:
    // Email link-la receive panna secure reset token.
    //
    // Input:
    // Random password reset token.
    //
    // Reason:
    // Correct user email ownership prove panna.
    //
    // Security:
    // Database-la raw token save panna maatom.
    // Token hash mattum save pannuvom.

    [Required]
    [MaxLength(500)]
    public string Token { get; set; }
        = string.Empty;


    // =========================================================
    // NEW PASSWORD
    // =========================================================
    //
    // Function:
    // User set panna new password.
    //
    // Input:
    // New plain password.
    //
    // Reason:
    // Old forgotten password replace panna.
    //
    // Security:
    // Plain password DB-la save panna maatom.
    // Existing PasswordHasher use panni hash pannuvom.

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string NewPassword { get; set; }
        = string.Empty;


    // =========================================================
    // CONFIRM PASSWORD
    // =========================================================
    //
    // Function:
    // NewPassword correctly type pannirukangala
    // confirm panna.
    //
    // Input:
    // Same new password second time.
    //
    // Output:
    // NewPassword match aagala-na
    // ASP.NET Core automatic validation error return pannum.

    [Required]
    [Compare(
        nameof(NewPassword),
        ErrorMessage =
            "Password and confirm password do not match.")]
    public string ConfirmPassword { get; set; }
        = string.Empty;
}