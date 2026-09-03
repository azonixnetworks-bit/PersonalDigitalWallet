using PersonalDigitalVault.Api.DTOs.Admin;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Services;

public class AdminSubscriptionService : IAdminSubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AdminSubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        IUserRepository userRepository,
        IConfiguration configuration)
    {
        _subscriptionRepository = subscriptionRepository;
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<AdminSubscriptionSummaryDto> GetSummaryAsync()
    {
        var users = await _userRepository.GetAllAsync();

        var normalUsers = users
            .Where(x => string.Equals(
                x.Role,
                "User",
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        var subscriptions =
            (await _subscriptionRepository.GetAllForAdminAsync())
            .Where(IsSupportedProvider)
            .ToList();

        var premiumUserIds = subscriptions
            .Where(IsValidActivePremium)
            .Select(x => x.UserId)
            .Distinct()
            .ToHashSet();

        int premiumUsers =
            normalUsers.Count(x => premiumUserIds.Contains(x.Id));

        int freeUsers =
            Math.Max(0, normalUsers.Count - premiumUsers);

        return new AdminSubscriptionSummaryDto
        {
            TotalUsers = normalUsers.Count,
            PremiumUsers = premiumUsers,
            FreeUsers = freeUsers,
            TotalSubscriptionRecords = subscriptions.Count,

            ActiveSubscriptions = subscriptions.Count(x =>
                StatusEquals(x, "ACTIVE") ||
                StatusEquals(x, "TRIALING")),

            CancelledSubscriptions = subscriptions.Count(x =>
                StatusEquals(x, "CANCELLED") ||
                StatusEquals(x, "CANCELED")),

            SuspendedSubscriptions = subscriptions.Count(x =>
                StatusEquals(x, "SUSPENDED") ||
                StatusEquals(x, "PAST_DUE") ||
                StatusEquals(x, "UNPAID") ||
                StatusEquals(x, "PAUSED")),

            ExpiredSubscriptions = subscriptions.Count(x =>
                StatusEquals(x, "EXPIRED") ||
                StatusEquals(x, "INCOMPLETE_EXPIRED"))
        };
    }

    public async Task<List<AdminSubscriptionDto>> GetAllAsync()
    {
        var subscriptions =
            await _subscriptionRepository.GetAllForAdminAsync();

        var result = new List<AdminSubscriptionDto>();

        foreach (var subscription in subscriptions)
        {
            if (subscription.User == null ||
                !string.Equals(
                    subscription.User.Role,
                    "User",
                    StringComparison.OrdinalIgnoreCase) ||
                !IsSupportedProvider(subscription))
            {
                continue;
            }

            result.Add(new AdminSubscriptionDto
            {
                UserId = subscription.UserId,
                UserName = subscription.User.FullName,
                MaskedEmail = MaskEmail(subscription.User.Email),
                Provider = subscription.Provider,
                PlanName = subscription.PlanName,
                Status = NormalizeStatus(subscription.Status),
                IsPremium = IsValidActivePremium(subscription),

                PayPalSubscriptionReference =
                    string.Equals(
                        subscription.Provider,
                        "PayPal",
                        StringComparison.OrdinalIgnoreCase)
                    ? MaskReference(subscription.PayPalSubscriptionId)
                    : "-",

                StripeSubscriptionReference =
                    string.Equals(
                        subscription.Provider,
                        "Stripe",
                        StringComparison.OrdinalIgnoreCase)
                    ? MaskReference(subscription.StripeSubscriptionId)
                    : "-",

                CancelAtPeriodEnd =
                    subscription.CancelAtPeriodEnd,

                StartDate = subscription.StartDate,
                NextBillingDate = subscription.NextBillingDate,
                CancelledAt = subscription.CancelledAt,
                LastVerifiedAt = subscription.LastVerifiedAt,
                CreatedAt = subscription.CreatedAt,
                UpdatedAt = subscription.UpdatedAt
            });
        }

        return result;
    }

    private bool IsValidActivePremium(Subscription subscription)
    {
        if (string.Equals(
            subscription.Provider,
            "PayPal",
            StringComparison.OrdinalIgnoreCase))
        {
            string? planId =
                _configuration["PayPal:PlanId"]?.Trim();

            return StatusEquals(subscription, "ACTIVE") &&
                   !string.IsNullOrWhiteSpace(planId) &&
                   string.Equals(
                       subscription.PayPalPlanId,
                       planId,
                       StringComparison.Ordinal);
        }

        if (string.Equals(
            subscription.Provider,
            "Stripe",
            StringComparison.OrdinalIgnoreCase))
        {
            string? priceId =
                _configuration["Stripe:PriceId"]?.Trim();

            bool validStatus =
                StatusEquals(subscription, "ACTIVE") ||
                StatusEquals(subscription, "TRIALING");

            return validStatus &&
                   !string.IsNullOrWhiteSpace(priceId) &&
                   string.Equals(
                       subscription.StripePriceId,
                       priceId,
                       StringComparison.Ordinal);
        }

        return false;
    }

    private static bool IsSupportedProvider(Subscription subscription)
    {
        return string.Equals(
                   subscription.Provider,
                   "PayPal",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   subscription.Provider,
                   "Stripe",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool StatusEquals(
        Subscription subscription,
        string expected)
    {
        return string.Equals(
            subscription.Status,
            expected,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "-";
        }

        string cleanEmail = email.Trim();
        int atIndex = cleanEmail.IndexOf('@');

        if (atIndex <= 0)
        {
            return "***";
        }

        return $"{cleanEmail[0]}***{cleanEmail.Substring(atIndex)}";
    }

    private static string MaskReference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        string clean = value.Trim();

        if (clean.Length <= 8)
        {
            return "****";
        }

        return $"{clean.Substring(0, 4)}****{clean.Substring(clean.Length - 4)}";
    }

    private static string NormalizeStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "UNKNOWN"
            : status.Trim().ToUpperInvariant();
    }
}
