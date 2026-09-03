namespace PersonalDigitalVault.Api.DTOs.Subscription;

public class StripePriceDetailsDto
{
    public string PriceId { get; set; } = string.Empty;

    public string ProductId { get; set; } = string.Empty;

    public bool Active { get; set; }

    public string Currency { get; set; } = string.Empty;

    public long? UnitAmount { get; set; }

    public string RecurringInterval { get; set; } = string.Empty;

    public long RecurringIntervalCount { get; set; } = 1;

    public bool IsRecurring => !string.IsNullOrWhiteSpace(RecurringInterval);
}
