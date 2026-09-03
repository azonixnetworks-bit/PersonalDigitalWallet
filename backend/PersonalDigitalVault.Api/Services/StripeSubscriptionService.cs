using PersonalDigitalVault.Api.DTOs.Subscription;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;

namespace PersonalDigitalVault.Api.Services;

public class StripeSubscriptionService : IStripeSubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IStripeService _stripeService;
    private readonly StripeSubscriptionSynchronizer _synchronizer;
    private readonly IUserRepository _userRepository;
    private readonly CurrentUserService _currentUser;
    private readonly IConfiguration _configuration;

    public StripeSubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        IStripeService stripeService,
        StripeSubscriptionSynchronizer synchronizer,
        IUserRepository userRepository,
        CurrentUserService currentUser,
        IConfiguration configuration)
    {
        _subscriptionRepository = subscriptionRepository;
        _stripeService = stripeService;
        _synchronizer = synchronizer;
        _userRepository = userRepository;
        _currentUser = currentUser;
        _configuration = configuration;
    }

    public async Task<StripeSubscriptionConfigDto> GetConfigAsync()
    {
        _ = await GetActiveCurrentUserAsync();

        // Fail closed when the server-side Stripe contract is incomplete.
        _ = GetRequiredConfiguration("Stripe:SecretKey");
        _ = GetRequiredConfiguration("Stripe:PriceId");
        _ = GetRequiredConfiguration("Stripe:WebhookSecret");
        _ = GetRequiredConfiguration("App:BaseUrl");

        StripePriceDetailsDto? price =
            await _stripeService.GetConfiguredPriceAsync();

        if (price == null || !price.Active || !price.IsRecurring)
        {
            throw new InvalidOperationException(
                "Configured Stripe Price is missing, inactive, or not recurring.");
        }

        string? expectedProductId =
            _configuration["Stripe:ProductId"]?.Trim();

        if (!string.IsNullOrWhiteSpace(expectedProductId) &&
            !string.Equals(
                price.ProductId,
                expectedProductId,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Configured Stripe Price does not belong to the expected Product.");
        }

        return new StripeSubscriptionConfigDto
        {
            Provider = "Stripe",
            PlanName = GetPlanName(),
            Environment = GetEnvironment(),
            Currency = price.Currency,
            UnitAmount = price.UnitAmount,
            BillingInterval = price.RecurringInterval,
            BillingIntervalCount = price.RecurringIntervalCount
        };
    }

    public async Task<SubscriptionDto?> GetMineAsync()
    {
        User user = await GetActiveCurrentUserAsync();

        Subscription? subscription =
            await _subscriptionRepository
                .GetLatestByUserIdAndProviderAsync(
                    user.Id,
                    "Stripe");

        return subscription == null
            ? null
            : MapToDto(subscription);
    }

    public async Task<CreateCheckoutSessionResponseDto>
        CreateCheckoutSessionAsync()
    {
        // A recurring payment must not be started if webhook lifecycle
        // synchronization is not configured.
        _ = GetRequiredConfiguration("Stripe:WebhookSecret");

        User user = await GetActiveCurrentUserAsync();

        if (await HasAnyActivePremiumAsync(user.Id))
        {
            throw new InvalidOperationException(
                "An active Premium subscription already exists for this account.");
        }

        Subscription? latestStripe =
            await _subscriptionRepository
                .GetLatestByUserIdAndProviderAsync(
                    user.Id,
                    "Stripe");

        if (latestStripe != null &&
            IsBillingProblemStatus(latestStripe.Status) &&
            !string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            throw new InvalidOperationException(
                "This Stripe subscription requires billing attention. Use Manage Billing instead.");
        }

        string customerId;

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            customerId =
                await _stripeService.CreateCustomerAsync(user);

            user.StripeCustomerId = customerId;
            await _userRepository.UpdateAsync(user);
        }
        else
        {
            customerId = user.StripeCustomerId.Trim();
        }

        StripeCheckoutSessionDetailsDto session =
            await _stripeService
                .CreateCheckoutSessionAsync(
                    user,
                    customerId);

        if (!string.Equals(
                session.CustomerId,
                customerId,
                StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException(
                "Stripe Checkout Session validation failed.");
        }

        return new CreateCheckoutSessionResponseDto
        {
            CheckoutUrl = session.Url
        };
    }

    public async Task<SubscriptionDto?> VerifyCheckoutSessionAsync(
        VerifyCheckoutSessionRequestDto request)
    {
        User user = await GetActiveCurrentUserAsync();

        if (request == null ||
            string.IsNullOrWhiteSpace(request.SessionId))
        {
            return null;
        }

        StripeCheckoutSessionDetailsDto? session =
            await _stripeService.GetCheckoutSessionAsync(
                request.SessionId.Trim());

        if (session == null ||
            !string.Equals(
                session.Status,
                "COMPLETE",
                StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(session.SubscriptionId) ||
            string.IsNullOrWhiteSpace(session.CustomerId))
        {
            return null;
        }

        if (!int.TryParse(
                session.ClientReferenceId,
                out int referencedUserId) ||
            referencedUserId != user.Id)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId) ||
            !string.Equals(
                user.StripeCustomerId,
                session.CustomerId,
                StringComparison.Ordinal))
        {
            return null;
        }

        StripeSubscriptionDetailsDto? stripeSubscription =
            await _stripeService.GetSubscriptionAsync(
                session.SubscriptionId);

        if (stripeSubscription == null ||
            !string.Equals(
                stripeSubscription.CustomerId,
                user.StripeCustomerId,
                StringComparison.Ordinal))
        {
            return null;
        }

        Subscription? subscription =
            await _synchronizer.SyncAsync(
                stripeSubscription,
                user.Id);

        return subscription == null
            ? null
            : MapToDto(subscription);
    }

    public async Task<CreatePortalSessionResponseDto>
        CreatePortalSessionAsync()
    {
        User user = await GetActiveCurrentUserAsync();

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            throw new InvalidOperationException(
                "No Stripe billing profile exists for this account.");
        }

        string portalUrl =
            await _stripeService.CreatePortalSessionAsync(
                user.StripeCustomerId.Trim());

        return new CreatePortalSessionResponseDto
        {
            PortalUrl = portalUrl
        };
    }

    private async Task<User> GetActiveCurrentUserAsync()
    {
        int userId = _currentUser.UserId;

        User? user =
            await _userRepository.GetByIdAsync(userId);

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "The current account is not available.");
        }

        if (!string.Equals(
                user.Role,
                "User",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Access denied.");
        }

        if (!user.IsEmailVerified ||
            !user.IsTotpEnabled ||
            string.IsNullOrWhiteSpace(user.TotpSecretEncrypted))
        {
            throw new UnauthorizedAccessException(
                "The current account security setup is incomplete.");
        }

        return user;
    }

    private async Task<bool> HasAnyActivePremiumAsync(int userId)
    {
        List<Subscription> subscriptions =
            await _subscriptionRepository.GetByUserIdAsync(userId);

        return subscriptions.Any(IsValidPremiumSubscription);
    }

    private bool IsValidPremiumSubscription(
        Subscription subscription)
    {
        if (string.Equals(
                subscription.Provider,
                "Stripe",
                StringComparison.OrdinalIgnoreCase))
        {
            bool validStripeStatus =
                string.Equals(
                    subscription.Status,
                    "ACTIVE",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    subscription.Status,
                    "TRIALING",
                    StringComparison.OrdinalIgnoreCase);

            string? expectedPriceId =
                _configuration["Stripe:PriceId"]?.Trim();

            return validStripeStatus &&
                   !string.IsNullOrWhiteSpace(expectedPriceId) &&
                   string.Equals(
                       subscription.StripePriceId,
                       expectedPriceId,
                       StringComparison.Ordinal);
        }

        if (string.Equals(
                subscription.Provider,
                "PayPal",
                StringComparison.OrdinalIgnoreCase))
        {
            string? expectedPlanId =
                _configuration["PayPal:PlanId"]?.Trim();

            return string.Equals(
                       subscription.Status,
                       "ACTIVE",
                       StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(expectedPlanId) &&
                   string.Equals(
                       subscription.PayPalPlanId,
                       expectedPlanId,
                       StringComparison.Ordinal);
        }

        return false;
    }

    private bool IsValidStripePremiumSubscription(
        Subscription subscription)
    {
        return string.Equals(
                   subscription.Provider,
                   "Stripe",
                   StringComparison.OrdinalIgnoreCase) &&
               IsValidPremiumSubscription(subscription);
    }

    private static bool IsBillingProblemStatus(string? status)
    {
        return string.Equals(
                   status,
                   "INCOMPLETE",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   status,
                   "PAST_DUE",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   status,
                   "UNPAID",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   status,
                   "PAUSED",
                   StringComparison.OrdinalIgnoreCase);
    }

    private SubscriptionDto MapToDto(
        Subscription subscription)
    {
        return new SubscriptionDto
        {
            Id = subscription.Id,
            Provider = subscription.Provider,
            PayPalSubscriptionId =
                subscription.PayPalSubscriptionId,
            PayPalPlanId =
                subscription.PayPalPlanId,
            StripeSubscriptionId =
                subscription.StripeSubscriptionId,
            StripePriceId =
                subscription.StripePriceId,
            PlanName = subscription.PlanName,
            Status = NormalizeStatus(subscription.Status),
            IsPremium =
                IsValidStripePremiumSubscription(subscription),
            CancelAtPeriodEnd =
                subscription.CancelAtPeriodEnd,
            StartDate = subscription.StartDate,
            NextBillingDate =
                subscription.NextBillingDate,
            CancelledAt = subscription.CancelledAt,
            LastVerifiedAt =
                subscription.LastVerifiedAt,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt
        };
    }

    private string GetRequiredConfiguration(string key)
    {
        string? value = _configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required Stripe configuration '{key}' is missing.");
        }

        return value.Trim();
    }

    private string GetPlanName()
    {
        return _configuration["Stripe:PlanName"]?.Trim()
            ?? "Premium Monthly";
    }

    private string GetEnvironment()
    {
        return _configuration["Stripe:Environment"]?.Trim()
            ?? "Sandbox";
    }

    private static string NormalizeStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "UNKNOWN"
            : status.Trim().ToUpperInvariant();
    }
}
