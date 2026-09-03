using System.ComponentModel.DataAnnotations;

namespace PersonalDigitalVault.Api.DTOs.Subscription;

public class ConfirmSubscriptionRequestDto
{
    // =========================================================
    // PAYPAL SUBSCRIPTION ID
    // =========================================================
    //
    // Function:
    // PayPal subscription approval mudinja piragu
    // frontend receive pannura Subscription ID.
    //
    // Example:
    // I-ABC123XYZ
    //
    // Input:
    // Frontend -> Backend
    //
    // IMPORTANT SECURITY:
    //
    // Indha ID frontend sonnadhu mattum trust panna maatom.
    //
    // Backend:
    //
    // SubscriptionId
    //      ↓
    // PayPal API
    //      ↓
    // Real subscription retrieve
    //      ↓
    // Plan ID verify
    //      ↓
    // Billing reference verify
    //      ↓
    // Status verify
    //
    // Success aana mattum local database save pannuvom.
    [Required]
    [StringLength(100)]
    public string SubscriptionId { get; set; }
        = string.Empty;
}