namespace PersonalDigitalVault.Api.DTOs.Subscription;

public class SubscriptionEntitlementDto
{
    // =========================================================
    // CURRENT PLAN
    // =========================================================
    //
    // Output:
    //
    // Free
    // Premium Monthly
    public string PlanName { get; set; }
        = "Free";


    // =========================================================
    // PREMIUM STATUS
    // =========================================================
    //
    // true:
    // Verified premium subscription from a supported provider
    // (PayPal ACTIVE or Stripe ACTIVE/TRIALING with configured plan/price).
    //
    // false:
    // Free / cancelled / suspended / expired / unverified plan.
    public bool IsPremium { get; set; }


    // =========================================================
    // MAXIMUM DOCUMENT COUNT
    // =========================================================
    //
    // Free    = 20
    // Premium = 200
    public int MaxDocuments { get; set; }


    // =========================================================
    // CURRENT DOCUMENT COUNT
    // =========================================================
    //
    // Current user database-la own pannura
    // document records count.
    public int CurrentDocuments { get; set; }


    // =========================================================
    // STORAGE LIMIT
    // =========================================================
    //
    // Bytes format-la save/return pannuvom.
    //
    // Reason:
    // Exact calculation easy.
    public long StorageLimitBytes { get; set; }


    // =========================================================
    // STORAGE USED
    // =========================================================
    //
    // Current user's documents FileSize total.
    public long StorageUsedBytes { get; set; }


    // =========================================================
    // STORAGE REMAINING
    // =========================================================
    //
    // StorageLimit - StorageUsed
    public long StorageRemainingBytes { get; set; }


    // =========================================================
    // MAXIMUM SINGLE FILE SIZE
    // =========================================================
    //
    // Existing PDV security rule:
    // 10 MB per file.
    public long MaxUploadBytes { get; set; }


    // =========================================================
    // CAN UPLOAD
    // =========================================================
    //
    // true:
    // Document count + storage innum available.
    //
    // Actual incoming file size check
    // upload time-la separate-aa nadakkum.
    public bool CanUpload { get; set; }


    // =========================================================
    // ACTIVE PREMIUM PROVIDER METADATA
    // =========================================================
    //
    // Dashboard/billing UX-ku safe lifecycle metadata only.
    // Payment credentials/secrets are never returned.
    public string? Provider { get; set; }

    public string? Status { get; set; }

    public DateTime? NextBillingDate { get; set; }

    public bool CancelAtPeriodEnd { get; set; }
}