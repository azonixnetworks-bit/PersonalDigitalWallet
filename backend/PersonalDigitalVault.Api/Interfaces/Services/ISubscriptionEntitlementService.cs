using PersonalDigitalVault.Api.DTOs.Subscription;

namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface ISubscriptionEntitlementService
{
    // =========================================================
    // GET CURRENT USER PLAN LIMITS
    // =========================================================
    //
    // Function:
    // Current authenticated user:
    //
    // Free?
    // Premium?
    // Storage used?
    // Storage limit?
    // Document count?
    //
    // ellam calculate pannum.
    //
    // Output:
    // SubscriptionEntitlementDto.
    Task<SubscriptionEntitlementDto>
        GetCurrentAsync();


    // =========================================================
    // CHECK DOCUMENT UPLOAD
    // =========================================================
    //
    // Input:
    // New upload file size in bytes.
    //
    // Output:
    //
    // null
    //      -> upload allowed.
    //
    // string message
    //      -> plan limit exceeded.
    //
    // Important:
    // Frontend decision trust panna maatom.
    // Backend upload service dhaan indha method call pannum.
    Task<string?>
        GetUploadBlockReasonAsync(
            long incomingFileSize);
}