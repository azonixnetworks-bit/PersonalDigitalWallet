namespace PersonalDigitalVault.Api.DTOs.Subscription;

public class SubscriptionConfigDto
{
    // =========================================================
    // PAYPAL CLIENT ID
    // =========================================================
    //
    // Function:
    // Frontend PayPal JavaScript SDK initialize panna use pannum.
    //
    // IMPORTANT:
    // Client ID public configuration.
    //
    // PayPal ClientSecret inga NEVER return panna koodathu.
    public string ClientId { get; set; }
        = string.Empty;


    // =========================================================
    // PAYPAL PLAN ID
    // =========================================================
    //
    // Function:
    // Frontend PayPal subscription create pannumbodhu
    // entha billing plan use panna vendum nu identify pannum.
    public string PlanId { get; set; }
        = string.Empty;


    // =========================================================
    // PLAN NAME
    // =========================================================
    //
    // Example:
    // Premium Monthly
    //
    // Subscription page-la display panna use pannuvom.
    public string PlanName { get; set; }
        = string.Empty;


    // =========================================================
    // ENVIRONMENT
    // =========================================================
    //
    // Values:
    // Sandbox
    // Live
    //
    // Development-la Sandbox use pannuvom.
    public string Environment { get; set; }
        = "Sandbox";


    // =========================================================
    // BILLING REFERENCE
    // =========================================================
    //
    // Function:
    // Current authenticated PDV user-ai
    // PayPal subscription-oda bind panna use pannuvom.
    //
    // Example:
    // PDV-USER-25
    //
    // IMPORTANT:
    // Backend current authenticated user-lendhu
    // indha value generate pannum.
    //
    // Frontend arbitrary reference decide panna koodathu.
    public string BillingReference { get; set; }
        = string.Empty;
}