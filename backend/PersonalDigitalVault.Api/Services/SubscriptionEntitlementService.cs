using PersonalDigitalVault.Api.DTOs.Subscription;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;

namespace PersonalDigitalVault.Api.Services;

public class SubscriptionEntitlementService
    : ISubscriptionEntitlementService
{
    // =========================================================
    // PLAN LIMIT CONSTANTS
    // =========================================================
    //
    // These are PDV project business rules.
    //
    // Later supervisor wants different values-na
    // one place-la mattum change panna mudiyum.


    // ---------------------------------------------------------
    // MAXIMUM SINGLE FILE
    // ---------------------------------------------------------
    //
    // Existing FileValidator rule-oda same.
    //
    // 10 * 1024 * 1024
    // = 10 MB.
    private const long MaxUploadBytes =
        10L * 1024L * 1024L;


    // ---------------------------------------------------------
    // FREE PLAN
    // ---------------------------------------------------------

    private const int FreeMaxDocuments =
        20;

    private const long FreeStorageBytes =
        50L * 1024L * 1024L;


    // ---------------------------------------------------------
    // PREMIUM PLAN
    // ---------------------------------------------------------

    private const int PremiumMaxDocuments =
        200;

    private const long PremiumStorageBytes =
        500L * 1024L * 1024L;


    // =========================================================
    // DEPENDENCIES
    // =========================================================

    private readonly ISubscriptionRepository
        _subscriptionRepository;


    private readonly IDocumentRepository
        _documentRepository;


    private readonly CurrentUserService
        _currentUser;


    private readonly IConfiguration
        _configuration;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================
    //
    // Input:
    //
    // Subscription repository
    // Document repository
    // CurrentUserService
    // Configuration
    //
    // Why:
    //
    // Current User
    //      ↓
    // Subscription Status
    //      +
    // Documents
    //      ↓
    // Current entitlement calculate panna.
    public SubscriptionEntitlementService(
        ISubscriptionRepository subscriptionRepository,
        IDocumentRepository documentRepository,
        CurrentUserService currentUser,
        IConfiguration configuration)
    {
        _subscriptionRepository =
            subscriptionRepository;

        _documentRepository =
            documentRepository;

        _currentUser =
            currentUser;

        _configuration =
            configuration;
    }


    // =========================================================
    // GET CURRENT ENTITLEMENTS
    // =========================================================

    public async Task<SubscriptionEntitlementDto>
        GetCurrentAsync()
    {
        // =====================================================
        // CURRENT USER
        // =====================================================
        //
        // Browser-lendhu UserId accept panna maatom.
        //
        // Validated JWT claim dhaan authority.

        int userId =
            _currentUser.UserId;


        // =====================================================
        // SUBSCRIPTION
        // =====================================================

        List<Subscription> subscriptions =
            await _subscriptionRepository
                .GetByUserIdAsync(
                    userId);


        Subscription? subscription =
            subscriptions
                .FirstOrDefault(
                    IsValidPremiumSubscription);


        bool isPremium =
            subscription != null;


        // =====================================================
        // CURRENT USER DOCUMENTS
        // =====================================================

        List<Document> documents =
            await _documentRepository
                .GetByUserAsync(
                    userId);


        int currentDocumentCount =
            documents.Count;


        // =====================================================
        // STORAGE USED
        // =====================================================
        //
        // Document.FileSize plaintext original file size.
        //
        // Existing DocumentService already save pannuthu.

        long storageUsed =
            documents.Sum(
                document =>
                    document.FileSize);


        // =====================================================
        // PLAN LIMITS
        // =====================================================

        int maxDocuments;

        long storageLimit;

        string planName;


        if (isPremium)
        {
            maxDocuments =
                PremiumMaxDocuments;

            storageLimit =
                PremiumStorageBytes;

            planName =
                string.IsNullOrWhiteSpace(
                    subscription!.PlanName)
                    ?
                    "Premium Monthly"
                    :
                    subscription.PlanName;
        }
        else
        {
            maxDocuments =
                FreeMaxDocuments;

            storageLimit =
                FreeStorageBytes;

            planName =
                "Free";
        }


        // =====================================================
        // REMAINING STORAGE
        // =====================================================

        long remainingStorage =
            storageLimit -
            storageUsed;


        if (remainingStorage < 0)
        {
            // Example:
            //
            // User Premium-la 100 MB use pannirukkar.
            // Then subscription cancel aaguthu.
            //
            // Free limit = 50 MB.
            //
            // Remaining negative-a frontend-ku
            // show panna vendam.
            remainingStorage =
                0;
        }


        // =====================================================
        // CAN UPLOAD?
        // =====================================================

        bool canUpload =
            currentDocumentCount <
                maxDocuments
            &&
            storageUsed <
                storageLimit;


        // =====================================================
        // OUTPUT
        // =====================================================

        return new SubscriptionEntitlementDto
        {
            PlanName =
                planName,

            IsPremium =
                isPremium,

            MaxDocuments =
                maxDocuments,

            CurrentDocuments =
                currentDocumentCount,

            StorageLimitBytes =
                storageLimit,

            StorageUsedBytes =
                storageUsed,

            StorageRemainingBytes =
                remainingStorage,

            MaxUploadBytes =
                MaxUploadBytes,

            CanUpload =
                canUpload,

            Provider =
                subscription?.Provider,

            Status =
                subscription?.Status,

            NextBillingDate =
                subscription?.NextBillingDate,

            CancelAtPeriodEnd =
                subscription?.CancelAtPeriodEnd
                ?? false
        };
    }


    // =========================================================
    // CHECK NEW DOCUMENT UPLOAD
    // =========================================================

    public async Task<string?>
        GetUploadBlockReasonAsync(
            long incomingFileSize)
    {
        // =====================================================
        // INVALID SIZE
        // =====================================================

        if (incomingFileSize <= 0)
        {
            return
                "The selected file is empty or invalid.";
        }


        // =====================================================
        // SINGLE FILE SECURITY LIMIT
        // =====================================================

        if (incomingFileSize >
            MaxUploadBytes)
        {
            return
                "The maximum allowed file size is 10 MB.";
        }


        // =====================================================
        // CURRENT ENTITLEMENT
        // =====================================================

        SubscriptionEntitlementDto entitlement =
            await GetCurrentAsync();


        // =====================================================
        // DOCUMENT COUNT LIMIT
        // =====================================================

        if (entitlement.CurrentDocuments >=
            entitlement.MaxDocuments)
        {
            if (entitlement.IsPremium)
            {
                return
                    "Your Premium document limit has been reached.";
            }


            return
                "Your Free plan document limit has been reached. Upgrade to Premium for more document storage.";
        }


        // =====================================================
        // TOTAL STORAGE LIMIT
        // =====================================================

        long storageAfterUpload;

        try
        {
            // checked:
            // long overflow accidentally nadakkaama protect pannum.
            storageAfterUpload =
                checked(
                    entitlement.StorageUsedBytes
                    +
                    incomingFileSize);
        }
        catch (OverflowException)
        {
            return
                "The storage calculation could not be completed.";
        }


        if (storageAfterUpload >
            entitlement.StorageLimitBytes)
        {
            if (entitlement.IsPremium)
            {
                return
                    "Your Premium storage limit has been reached.";
            }


            return
                "Your Free plan storage limit has been reached. Upgrade to Premium for more storage.";
        }


        // =====================================================
        // ALLOWED
        // =====================================================

        return null;
    }


    // =========================================================
    // VALID PREMIUM SUBSCRIPTION
    // =========================================================
    //
    // Premium-na database Status ACTIVE irukkaradhu mattum
    // blindly trust panna maatom.
    //
    // Verify:
    //
    // Provider = PayPal
    // Status   = ACTIVE
    // PlanId   = configured PayPal PlanId
    //
    // R7.5 + R7.8 PayPal backend verification already
    // local record sync pannuthu.
    private bool IsValidPremiumSubscription(
        Subscription? subscription)
    {
        if (subscription == null)
        {
            return false;
        }

        if (string.Equals(
            subscription.Provider,
            "PayPal",
            StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(
                subscription.Status,
                "ACTIVE",
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string? configuredPlanId =
                _configuration["PayPal:PlanId"]?.Trim();

            return !string.IsNullOrWhiteSpace(configuredPlanId) &&
                   string.Equals(
                       subscription.PayPalPlanId,
                       configuredPlanId,
                       StringComparison.Ordinal);
        }

        if (string.Equals(
            subscription.Provider,
            "Stripe",
            StringComparison.OrdinalIgnoreCase))
        {
            bool validStatus =
                string.Equals(
                    subscription.Status,
                    "ACTIVE",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    subscription.Status,
                    "TRIALING",
                    StringComparison.OrdinalIgnoreCase);

            if (!validStatus)
            {
                return false;
            }

            string? configuredPriceId =
                _configuration["Stripe:PriceId"]?.Trim();

            return !string.IsNullOrWhiteSpace(configuredPriceId) &&
                   string.Equals(
                       subscription.StripePriceId,
                       configuredPriceId,
                       StringComparison.Ordinal);
        }

        return false;
    }
}
