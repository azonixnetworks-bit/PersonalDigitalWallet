namespace PersonalDigitalVault.Api.Entities;

public class Subscription
{
    public int Id { get; set; }
    public int UserId { get; set; }

    // Supported values: PayPal / Stripe.
    public string Provider { get; set; } = string.Empty;

    // Existing PayPal references.
    public string? PayPalSubscriptionId { get; set; }
    public string? PayPalPlanId { get; set; }

    // Stripe references. Nullable because PayPal rows do not use them.
    public string? StripeSubscriptionId { get; set; }
    public string? StripePriceId { get; set; }

    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }
    public DateTime? NextBillingDate { get; set; }

    // Stripe only. PayPal rows remain false.
    public bool CancelAtPeriodEnd { get; set; }

    public DateTime? CancelledAt { get; set; }
    public DateTime? LastVerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
