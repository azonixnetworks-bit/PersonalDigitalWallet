namespace PersonalDigitalVault.Api.DTOs.Subscription;

public class StripeCheckoutSessionDetailsDto
{
    public string SessionId { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? CustomerId { get; set; }

    public string? SubscriptionId { get; set; }

    public string? ClientReferenceId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = string.Empty;

    public bool LiveMode { get; set; }
}
