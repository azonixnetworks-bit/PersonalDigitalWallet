namespace PersonalDigitalVault.Api.DTOs.Subscription;

public class PayPalSubscriptionDetailsDto
{
    // =========================================================
    // PAYPAL SUBSCRIPTION ID
    // =========================================================
    //
    // Function:
    // PayPal create pannina subscription unique ID.
    //
    // Example:
    // I-ABC123XYZ
    //
    // Output:
    // PayPal API response-lendhu varum.
    public string SubscriptionId { get; set; }
        = string.Empty;


    // =========================================================
    // PAYPAL PLAN ID
    // =========================================================
    //
    // Function:
    // Subscription entha PayPal billing plan use pannuthu
    // nu identify pannum.
    //
    // Later:
    // Config PlanId-oda compare pannuvom.
    public string PlanId { get; set; }
        = string.Empty;


    // =========================================================
    // STATUS
    // =========================================================
    //
    // PayPal possible status examples:
    //
    // APPROVAL_PENDING
    // APPROVED
    // ACTIVE
    // SUSPENDED
    // CANCELLED
    // EXPIRED
    //
    // Premium access-ku ACTIVE mattum use pannuvom.
    public string Status { get; set; }
        = string.Empty;


    // =========================================================
    // CUSTOM ID / BILLING REFERENCE
    // =========================================================
    //
    // Function:
    // PayPal subscription create pannumbodhu
    // namma application send pannura custom_id.
    //
    // Example:
    //
    // PDV-USER-8
    //
    // Security:
    // Current authenticated user-oda expected
    // billing reference-oda compare panna use pannuvom.
    public string? BillingReference { get; set; }


    // =========================================================
    // START DATE
    // =========================================================
    //
    // Subscription PayPal-la start aana UTC time.
    public DateTime? StartDate { get; set; }


    // =========================================================
    // NEXT BILLING DATE
    // =========================================================
    //
    // Next recurring payment expected date.
    //
    // Cancelled / expired subscription-ku
    // null-a irukkalaam.
    public DateTime? NextBillingDate { get; set; }
}