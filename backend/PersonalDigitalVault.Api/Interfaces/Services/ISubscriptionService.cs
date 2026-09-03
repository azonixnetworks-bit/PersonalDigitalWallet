using PersonalDigitalVault.Api.DTOs.Subscription;

namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface ISubscriptionService
{
    // =========================================================
    // GET FRONTEND PAYPAL CONFIG
    // =========================================================
    //
    // Function:
    // Current authenticated User-ku
    // PayPal public subscription configuration return pannum.
    //
    // Output:
    // ClientId
    // PlanId
    // PlanName
    // Environment
    // BillingReference
    //
    // Security:
    // ClientSecret / WebhookId return panna koodathu.
    Task<SubscriptionConfigDto>
        GetConfigAsync();


    // =========================================================
    // GET CURRENT USER SUBSCRIPTION
    // =========================================================
    //
    // Function:
    // Current logged-in User-ku latest subscription
    // database-lendhu edukkum.
    //
    // Output:
    // SubscriptionDto
    // or
    // null -> subscription illa.
    Task<SubscriptionDto?>
        GetMineAsync();


    // =========================================================
    // CONFIRM PAYPAL SUBSCRIPTION
    // =========================================================
    //
    // Function:
    // Frontend PayPal approval piragu kudukkura
    // SubscriptionId-ai PayPal server-la verify pannum.
    //
    // Security checks:
    //
    // Subscription ID match?
    // Plan ID match?
    // Billing reference current User-oda match?
    // Status ACTIVE-aa?
    //
    // All success aana mattum DB save pannum.
    Task<SubscriptionDto?>
        ConfirmAsync(
            ConfirmSubscriptionRequestDto request);


    // =========================================================
    // CANCEL CURRENT SUBSCRIPTION
    // =========================================================
    //
    // Function:
    // Current authenticated User-oda PayPal
    // subscription mattum cancel pannum.
    //
    // Output:
    // true  -> success
    // false -> fail / no subscription
    Task<bool>
        CancelAsync();
}