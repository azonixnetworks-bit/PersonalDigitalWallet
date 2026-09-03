namespace PersonalDigitalVault.Api.DTOs.Subscription;

public class StripeSubscriptionDetailsDto
{
    public string SubscriptionId { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public string PriceId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int? MetadataUserId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? NextBillingDate { get; set; }

    public bool CancelAtPeriodEnd { get; set; }

    public DateTime? CancelledAt { get; set; }

    public bool LiveMode { get; set; }
}
