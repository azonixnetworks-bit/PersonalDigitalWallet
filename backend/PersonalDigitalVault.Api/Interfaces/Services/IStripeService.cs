using PersonalDigitalVault.Api.DTOs.Subscription;
using PersonalDigitalVault.Api.Entities;

namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface IStripeService
{
    Task<string> CreateCustomerAsync(User user);

    Task<StripePriceDetailsDto?> GetConfiguredPriceAsync();

    Task<StripeCheckoutSessionDetailsDto> CreateCheckoutSessionAsync(
        User user,
        string stripeCustomerId);

    Task<StripeCheckoutSessionDetailsDto?> GetCheckoutSessionAsync(string sessionId);

    Task<StripeSubscriptionDetailsDto?> GetSubscriptionAsync(string subscriptionId);

    Task<string> CreatePortalSessionAsync(string stripeCustomerId);
}
