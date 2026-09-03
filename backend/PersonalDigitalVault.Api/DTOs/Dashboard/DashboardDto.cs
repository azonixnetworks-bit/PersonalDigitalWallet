namespace PersonalDigitalVault.Api.DTOs.Dashboard;

public class DashboardDto
{
    public DashboardUserDto User { get; set; } = new();
    public DashboardSummaryDto Summary { get; set; } = new();
    public DashboardStorageDto Storage { get; set; } = new();
    public DashboardPlanDto Subscription { get; set; } = new();
    public DashboardSecurityDto Security { get; set; } = new();
    public List<DashboardRecentDocumentDto> RecentDocuments { get; set; } = new();
    public List<DashboardFolderDto> RecentFolders { get; set; } = new();
}

public class DashboardUserDto
{
    public string FullName { get; set; } = string.Empty;
}

public class DashboardSummaryDto
{
    public int FolderCount { get; set; }
    public int DocumentCount { get; set; }
    public int CredentialCount { get; set; }
    public int SharedWithMeCount { get; set; }
}

public class DashboardStorageDto
{
    public long UsedBytes { get; set; }
    public long LimitBytes { get; set; }
    public long RemainingBytes { get; set; }
    public double UsagePercent { get; set; }
    public int CurrentDocuments { get; set; }
    public int MaxDocuments { get; set; }
    public bool CanUpload { get; set; }
}

public class DashboardPlanDto
{
    public string PlanName { get; set; } = "Free";
    public bool IsPremium { get; set; }
    public string? Provider { get; set; }
    public string? Status { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public string PrimaryActionUrl { get; set; } = "/html/stripe-subscription.html";
    public string PrimaryActionText { get; set; } = "Upgrade with Stripe";
    public string? SecondaryActionUrl { get; set; } = "/html/subscription.html";
    public string? SecondaryActionText { get; set; } = "PayPal option";
}

public class DashboardSecurityDto
{
    public bool EmailVerified { get; set; }
    public bool TotpEnabled { get; set; }
    public bool EncryptedStorageActive { get; set; } = true;
    public string OverallStatus { get; set; } = "SECURE";
}

public class DashboardRecentDocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FolderName { get; set; } = "Root Vault";
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DashboardFolderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DocumentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
