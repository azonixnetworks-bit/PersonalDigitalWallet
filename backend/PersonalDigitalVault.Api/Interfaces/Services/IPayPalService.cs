using PersonalDigitalVault.Api.DTOs.Subscription;

namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface IPayPalService
{
    // =========================================================
    // GET PAYPAL SUBSCRIPTION
    // =========================================================
    //
    // Function:
    // Frontend kudukkura PayPal Subscription ID-a
    // PayPal server-kitta verify panni
    // real subscription details return pannum.
    //
    // Input:
    // subscriptionId
    //
    // Example:
    // I-ABC123XYZ
    //
    // Output:
    // PayPalSubscriptionDetailsDto
    //
    // or
    //
    // null
    //
    // IMPORTANT:
    // Frontend data direct trust panna maatom.
    Task<PayPalSubscriptionDetailsDto?>
        GetSubscriptionAsync(
            string subscriptionId);


    // =========================================================
    // CANCEL PAYPAL SUBSCRIPTION
    // =========================================================
    //
    // Function:
    // PayPal server-la existing subscription
    // cancel pannum.
    //
    // Input:
    //
    // subscriptionId
    // reason
    //
    // Output:
    //
    // true  -> PayPal cancellation success
    // false -> cancellation fail
    Task<bool> CancelSubscriptionAsync(
        string subscriptionId,
        string reason);
}