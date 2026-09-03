namespace PersonalDigitalVault.Api.DTOs.Admin;

public class AdminSubscriptionSummaryDto
{
    // Normal PDV User accounts count.
    public int TotalUsers { get; set; }


    // ACTIVE PayPal Premium users.
    public int PremiumUsers { get; set; }


    // Premium ACTIVE illaadha users.
    public int FreeUsers { get; set; }


    // Historical subscription records total.
    public int TotalSubscriptionRecords { get; set; }


    // Current DB status counts.
    public int ActiveSubscriptions { get; set; }

    public int CancelledSubscriptions { get; set; }

    public int SuspendedSubscriptions { get; set; }

    public int ExpiredSubscriptions { get; set; }
}