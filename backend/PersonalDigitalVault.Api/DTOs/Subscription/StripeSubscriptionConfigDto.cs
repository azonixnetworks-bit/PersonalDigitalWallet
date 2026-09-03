namespace PersonalDigitalVault.Api.DTOs.Subscription;

public class StripeSubscriptionConfigDto
{
    public string Provider { get; set; } = "Stripe";
    public string PlanName { get; set; } = string.Empty;
    public string Environment { get; set; } = "Sandbox";
    public string Currency { get; set; } = string.Empty;
    public long? UnitAmount { get; set; }
    public string BillingInterval { get; set; } = string.Empty;
    public long BillingIntervalCount { get; set; } = 1;
}
