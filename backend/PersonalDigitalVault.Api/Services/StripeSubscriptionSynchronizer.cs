using PersonalDigitalVault.Api.DTOs.Subscription;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Repositories;

namespace PersonalDigitalVault.Api.Services;

public class StripeSubscriptionSynchronizer
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public StripeSubscriptionSynchronizer(
        ISubscriptionRepository subscriptionRepository,
        IUserRepository userRepository,
        IConfiguration configuration)
    {
        _subscriptionRepository = subscriptionRepository;
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<Subscription?> SyncAsync(
        StripeSubscriptionDetailsDto stripe,
        int? expectedUserId = null)
    {
        if (string.IsNullOrWhiteSpace(stripe.SubscriptionId) ||
            string.IsNullOrWhiteSpace(stripe.CustomerId))
        {
            return null;
        }

        string environment = _configuration["Stripe:Environment"]?.Trim() ?? "Sandbox";
        bool expectLive = string.Equals(environment, "Live", StringComparison.OrdinalIgnoreCase);

        if (stripe.LiveMode != expectLive)
        {
            return null;
        }

        User? user = null;

        if (expectedUserId.HasValue)
        {
            user = await _userRepository.GetByIdAsync(expectedUserId.Value);

            if (user == null)
            {
                return null;
            }

            if (stripe.MetadataUserId.HasValue &&
                stripe.MetadataUserId.Value != user.Id)
            {
                return null;
            }
        }
        else if (stripe.MetadataUserId.HasValue)
        {
            user = await _userRepository.GetByIdAsync(stripe.MetadataUserId.Value);
        }

        user ??= await _userRepository.GetByStripeCustomerIdAsync(stripe.CustomerId);

        if (user == null ||
            !string.Equals(user.Role, "User", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // One Stripe Customer must never be silently rebound to another PDV user.
        if (!string.IsNullOrWhiteSpace(user.StripeCustomerId) &&
            !string.Equals(user.StripeCustomerId, stripe.CustomerId, StringComparison.Ordinal))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            user.StripeCustomerId = stripe.CustomerId;
            await _userRepository.UpdateAsync(user);
        }

        Subscription? subscription =
            await _subscriptionRepository.GetByStripeSubscriptionIdAsync(stripe.SubscriptionId);

        if (subscription != null && subscription.UserId != user.Id)
        {
            return null;
        }

        bool isNew = subscription == null;

        subscription ??= new Subscription
        {
            UserId = user.Id,
            StripeSubscriptionId = stripe.SubscriptionId,
            CreatedAt = DateTime.UtcNow
        };

        string expectedPriceId = GetRequiredConfiguration("Stripe:PriceId");
        string configuredPlanName =
            _configuration["Stripe:PlanName"]?.Trim() ?? "Premium Monthly";

        subscription.Provider = "Stripe";
        subscription.StripeSubscriptionId = stripe.SubscriptionId;
        subscription.StripePriceId = stripe.PriceId;
        subscription.PlanName = string.Equals(
            stripe.PriceId,
            expectedPriceId,
            StringComparison.Ordinal)
            ? configuredPlanName
            : "Stripe Subscription";
        subscription.Status = NormalizeStatus(stripe.Status);
        subscription.StartDate = stripe.StartDate;
        subscription.NextBillingDate = stripe.NextBillingDate;
        subscription.CancelAtPeriodEnd = stripe.CancelAtPeriodEnd;
        subscription.CancelledAt = stripe.CancelledAt;
        subscription.LastVerifiedAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        if (isNew)
        {
            await _subscriptionRepository.AddAsync(subscription);
        }
        else
        {
            await _subscriptionRepository.UpdateAsync(subscription);
        }

        return subscription;
    }

    private string GetRequiredConfiguration(string key)
    {
        string? value = _configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required Stripe configuration '{key}' is missing.");
        }

        return value.Trim();
    }

    private static string NormalizeStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "UNKNOWN"
            : status.Trim().ToUpperInvariant();
    }
}
