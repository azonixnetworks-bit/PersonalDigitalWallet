namespace PersonalDigitalVault.Api.DTOs.Admin;

public class AdminSubscriptionDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string MaskedEmail { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsPremium { get; set; }

    // Provider identifiers are masked before they leave the backend.
    public string PayPalSubscriptionReference { get; set; } = string.Empty;
    public string StripeSubscriptionReference { get; set; } = string.Empty;

    public bool CancelAtPeriodEnd { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
