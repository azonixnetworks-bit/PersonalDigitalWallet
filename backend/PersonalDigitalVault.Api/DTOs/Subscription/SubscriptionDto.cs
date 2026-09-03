namespace PersonalDigitalVault.Api.DTOs.Subscription;

public class SubscriptionDto
{
    public int Id { get; set; }

    public string Provider { get; set; } = string.Empty;

    // Existing PayPal response contract.
    public string? PayPalSubscriptionId { get; set; }
    public string? PayPalPlanId { get; set; }

    // Stripe response contract.
    public string? StripeSubscriptionId { get; set; }
    public string? StripePriceId { get; set; }

    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsPremium { get; set; }

    public bool CancelAtPeriodEnd { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
