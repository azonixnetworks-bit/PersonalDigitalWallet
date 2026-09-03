using PersonalDigitalVault.Api.DTOs.Subscription;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;

namespace PersonalDigitalVault.Api.Services;

public class SubscriptionService
    : ISubscriptionService
{
    // =========================================================
    // DEPENDENCIES
    // =========================================================

    // Subscription database operations.
    private readonly ISubscriptionRepository
        _subscriptionRepository;


    // PayPal REST API communication.
    private readonly IPayPalService
        _payPalService;


    // Current User database check.
    private readonly IUserRepository
        _userRepository;


    // JWT-lendhu current UserId read pannum.
    private readonly CurrentUserService
        _currentUser;


    // PayPal public/server configuration read pannum.
    private readonly IConfiguration
        _configuration;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================
    //
    // Input:
    //
    // Subscription Repository
    // PayPal Service
    // User Repository
    // CurrentUserService
    // IConfiguration
    //
    // Output:
    // Dependencies private variables-la store aagum.
    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        IPayPalService payPalService,
        IUserRepository userRepository,
        CurrentUserService currentUser,
        IConfiguration configuration)
    {
        _subscriptionRepository =
            subscriptionRepository;

        _payPalService =
            payPalService;

        _userRepository =
            userRepository;

        _currentUser =
            currentUser;

        _configuration =
            configuration;
    }


    // =========================================================
    // GET PAYPAL PUBLIC CONFIG
    // =========================================================
    //
    // Function:
    //
    // Current User validate
    //      ↓
    // PayPal public config read
    //      ↓
    // User-specific BillingReference create
    //      ↓
    // Frontend DTO return
    //
    // IMPORTANT:
    //
    // ClientSecret
    // WebhookId
    // OAuth AccessToken
    //
    // inga return panna maatom.
    public async Task<SubscriptionConfigDto>
        GetConfigAsync()
    {
        // =====================================================
        // CURRENT USER SECURITY CHECK
        // =====================================================

        User user =
            await GetActiveCurrentUserAsync();


        // =====================================================
        // PUBLIC PAYPAL CONFIGURATION
        // =====================================================

        string clientId =
            GetRequiredConfiguration(
                "PayPal:ClientId");


        string planId =
            GetRequiredConfiguration(
                "PayPal:PlanId");


        string planName =
            GetPlanName();


        string environment =
            GetEnvironment();


        // =====================================================
        // RETURN SAFE FRONTEND CONFIG
        // =====================================================

        return new SubscriptionConfigDto
        {
            ClientId =
                clientId,

            PlanId =
                planId,

            PlanName =
                planName,

            Environment =
                environment,

            BillingReference =
                BuildBillingReference(
                    user.Id)
        };
    }


    // =========================================================
    // GET CURRENT USER SUBSCRIPTION
    // =========================================================
    //
    // Function:
    // Current logged-in User-ku latest subscription
    // database-lendhu return pannum.
    //
    // Input:
    // Direct UserId input illa.
    //
    // UserId:
    // Validated JWT -> CurrentUserService.
    //
    // Output:
    // SubscriptionDto
    // or null.
    public async Task<SubscriptionDto?>
        GetMineAsync()
    {
        User user =
            await GetActiveCurrentUserAsync();


        Subscription? subscription =
            await _subscriptionRepository
                .GetLatestByUserIdAndProviderAsync(
                    user.Id,
                    "PayPal");


        if (subscription == null)
        {
            return null;
        }


        return MapToDto(
            subscription);
    }


    // =========================================================
    // CONFIRM PAYPAL SUBSCRIPTION
    // =========================================================
    //
    // Function:
    //
    // Frontend:
    // SubscriptionId
    //      ↓
    // Current User validation
    //      ↓
    // PayPal API verification
    //      ↓
    // Subscription ID check
    //      ↓
    // Plan ID check
    //      ↓
    // BillingReference check
    //      ↓
    // ACTIVE status check
    //      ↓
    // Duplicate check
    //      ↓
    // Save / Update DB
    //
    // Output:
    // SubscriptionDto
    // or null.
    public async Task<SubscriptionDto?>
        ConfirmAsync(
            ConfirmSubscriptionRequestDto request)
    {
        // =====================================================
        // CURRENT USER
        // =====================================================

        User user =
            await GetActiveCurrentUserAsync();


        // =====================================================
        // INPUT VALIDATION
        // =====================================================

        if (request == null ||
            string.IsNullOrWhiteSpace(
                request.SubscriptionId))
        {
            return null;
        }


        string subscriptionId =
            request.SubscriptionId
                .Trim();


        if (subscriptionId.Length < 3 ||
            subscriptionId.Length > 100)
        {
            return null;
        }


        // =====================================================
        // GET REAL SUBSCRIPTION FROM PAYPAL
        // =====================================================
        //
        // Frontend result direct trust panna maatom.

        PayPalSubscriptionDetailsDto? paypal =
            await _payPalService
                .GetSubscriptionAsync(
                    subscriptionId);


        if (paypal == null)
        {
            return null;
        }


        // =====================================================
        // VERIFY SUBSCRIPTION ID
        // =====================================================
        //
        // User submit panna ID-um
        // PayPal return panna ID-um same-a irukkanum.

        if (!string.Equals(
            subscriptionId,
            paypal.SubscriptionId,
            StringComparison.Ordinal))
        {
            return null;
        }


        // =====================================================
        // VERIFY PAYPAL PLAN
        // =====================================================

        string expectedPlanId =
            GetRequiredConfiguration(
                "PayPal:PlanId");


        if (!string.Equals(
            paypal.PlanId,
            expectedPlanId,
            StringComparison.Ordinal))
        {
            // Vera PayPal plan subscription use panni
            // premium activate panna koodathu.
            return null;
        }


        // =====================================================
        // VERIFY USER BILLING REFERENCE
        // =====================================================
        //
        // Example:
        //
        // Current JWT UserId:
        // 8
        //
        // Expected:
        // PDV-USER-8
        //
        // PayPal custom_id:
        // PDV-USER-8
        //
        // Match aana mattum continue.

        string expectedBillingReference =
            BuildBillingReference(
                user.Id);


        if (!string.Equals(
            paypal.BillingReference,
            expectedBillingReference,
            StringComparison.Ordinal))
        {
            // Someone else's PayPal Subscription ID
            // current account-la use panna block pannuvom.
            return null;
        }


        // =====================================================
        // VERIFY PAYPAL STATUS
        // =====================================================
        //
        // Premium activation:
        //
        // ACTIVE mattum.
        //
        // APPROVAL_PENDING / CANCELLED / SUSPENDED etc.
        // premium activate panna koodathu.

        if (!string.Equals(
            paypal.Status,
            "ACTIVE",
            StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }


        // =====================================================
        // CHECK SAME PAYPAL SUBSCRIPTION ALREADY EXISTS
        // =====================================================

        Subscription? existingSubscription =
            await _subscriptionRepository
                .GetByPayPalSubscriptionIdAsync(
                    paypal.SubscriptionId);


        if (existingSubscription != null)
        {
            // =================================================
            // OWNERSHIP CHECK
            // =================================================
            //
            // Same PayPal SubscriptionId vera PDV user-ku
            // already bind aagirundha current User-ku
            // transfer panna koodathu.

            if (existingSubscription.UserId !=
                user.Id)
            {
                return null;
            }


            // =================================================
            // UPDATE EXISTING RECORD
            // =================================================

            UpdateFromPayPal(
                existingSubscription,
                paypal);


            existingSubscription.PlanName =
                GetPlanName();


            existingSubscription.UpdatedAt =
                DateTime.UtcNow;


            await _subscriptionRepository
                .UpdateAsync(
                    existingSubscription);


            return MapToDto(
                existingSubscription);
        }


        // =====================================================
        // CHECK CURRENT USER ALREADY HAS ACTIVE SUBSCRIPTION
        // =====================================================
        //
        // Basic duplicate protection.
        //
        // User already active premium irundha
        // vera PayPal subscription-ai same PDV account-ku
        // attach panna maatom.

        Subscription? latestSubscription =
            await _subscriptionRepository
                .GetLatestByUserIdAndProviderAsync(
                    user.Id,
                    "PayPal");


        if (latestSubscription != null &&
            string.Equals(
                latestSubscription.Status,
                "ACTIVE",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                latestSubscription.PayPalSubscriptionId,
                paypal.SubscriptionId,
                StringComparison.Ordinal))
        {
            return null;
        }


        // =====================================================
        // CREATE LOCAL SUBSCRIPTION
        // =====================================================

        DateTime now =
            DateTime.UtcNow;


        var newSubscription =
            new Subscription
            {
                UserId =
                    user.Id,

                Provider =
                    "PayPal",

                PayPalSubscriptionId =
                    paypal.SubscriptionId,

                PayPalPlanId =
                    paypal.PlanId,

                PlanName =
                    GetPlanName(),

                Status =
                    NormalizeStatus(
                        paypal.Status),

                StartDate =
                    paypal.StartDate,

                NextBillingDate =
                    paypal.NextBillingDate,

                CancelledAt =
                    null,

                LastVerifiedAt =
                    now,

                CreatedAt =
                    now,

                UpdatedAt =
                    now
            };


        await _subscriptionRepository
            .AddAsync(
                newSubscription);


        return MapToDto(
            newSubscription);
    }


    // =========================================================
    // CANCEL CURRENT USER SUBSCRIPTION
    // =========================================================
    //
    // Function:
    //
    // Current User
    //      ↓
    // Latest own subscription
    //      ↓
    // PayPal subscription ID
    //      ↓
    // PayPal cancel API
    //      ↓
    // Local status CANCELLED
    //
    // Security:
    // Frontend subscriptionId accept panna maatom.
    //
    // Current user's DB subscription ID mattum
    // backend use pannum.
    public async Task<bool>
        CancelAsync()
    {
        // =====================================================
        // CURRENT USER SECURITY CHECK
        // =====================================================

        User user =
            await GetActiveCurrentUserAsync();


        // =====================================================
        // CURRENT SUBSCRIPTION
        // =====================================================

        Subscription? subscription =
            await _subscriptionRepository
                .GetLatestByUserIdAndProviderAsync(
                    user.Id,
                    "PayPal");


        if (subscription == null)
        {
            return false;
        }


        // =====================================================
        // PROVIDER CHECK
        // =====================================================

        if (!string.Equals(
            subscription.Provider,
            "PayPal",
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }


        // =====================================================
        // ALREADY FINISHED
        // =====================================================
        //
        // Cancel operation idempotent-aa irukka
        // already cancelled / expired-na success return pannalaam.

        if (string.Equals(
                subscription.Status,
                "CANCELLED",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                subscription.Status,
                "EXPIRED",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }


        if (string.IsNullOrWhiteSpace(
            subscription.PayPalSubscriptionId))
        {
            return false;
        }


        // =====================================================
        // CALL PAYPAL CANCEL API
        // =====================================================

        bool cancelled =
            await _payPalService
                .CancelSubscriptionAsync(
                    subscription.PayPalSubscriptionId,
                    "Cancelled by the Personal Digital Vault subscriber."
                );


        if (!cancelled)
        {
            // PayPal reject pannina
            // local database status fake-aa change panna maatom.
            return false;
        }


        // =====================================================
        // UPDATE LOCAL STATUS
        // =====================================================
        //
        // PayPal successful cancellation response vandha
        // local state update pannuvom.
        //
        // R7.8 webhook later server-to-server
        // confirmation/synchronization provide pannum.

        DateTime now =
            DateTime.UtcNow;


        subscription.Status =
            "CANCELLED";


        subscription.CancelledAt =
            now;


        subscription.NextBillingDate =
            null;


        subscription.LastVerifiedAt =
            now;


        subscription.UpdatedAt =
            now;


        await _subscriptionRepository
            .UpdateAsync(
                subscription);


        return true;
    }


    // =========================================================
    // GET ACTIVE CURRENT NORMAL USER
    // =========================================================
    //
    // Function:
    // JWT UserId use panni actual database User check pannum.
    //
    // Why:
    //
    // JWT valid-a irundhaalum:
    //
    // Admin account
    // Disabled account
    // Incomplete email/TOTP setup
    //
    // subscription feature use panna koodathu.
    //
    // Output:
    // Valid User entity.
    //
    // Failure:
    // UnauthorizedAccessException.
    private async Task<User>
        GetActiveCurrentUserAsync()
    {
        // =====================================================
        // JWT USER ID
        // =====================================================

        int userId =
            _currentUser.UserId;


        // =====================================================
        // DATABASE USER
        // =====================================================

        User? user =
            await _userRepository
                .GetByIdAsync(
                    userId);


        if (user == null ||
            !user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "The current account is not available."
            );
        }


        // =====================================================
        // NORMAL USER ONLY
        // =====================================================
        //
        // Admin subscription வாங்க வேண்டாம்.
        //
        // Admin private vault features use panna
        // koodathu-nu existing architecture preserve pannuvom.

        if (!string.Equals(
            user.Role,
            "User",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "Access denied."
            );
        }


        // =====================================================
        // SECURITY SETUP CHECK
        // =====================================================

        if (!user.IsEmailVerified ||
            !user.IsTotpEnabled ||
            string.IsNullOrWhiteSpace(
                user.TotpSecretEncrypted))
        {
            throw new UnauthorizedAccessException(
                "The current account security setup is incomplete."
            );
        }


        return user;
    }


    // =========================================================
    // UPDATE LOCAL ENTITY FROM PAYPAL
    // =========================================================
    //
    // Function:
    // PayPal verified response values local entity-ku map pannum.
    //
    // Input:
    //
    // Subscription entity
    // PayPal verified DTO
    //
    // Output:
    // Existing entity values updated.
    private static void UpdateFromPayPal(
        Subscription subscription,
        PayPalSubscriptionDetailsDto paypal)
    {
        subscription.PayPalPlanId =
            paypal.PlanId;


        subscription.Status =
            NormalizeStatus(
                paypal.Status);


        subscription.StartDate =
            paypal.StartDate;


        subscription.NextBillingDate =
            paypal.NextBillingDate;


        subscription.LastVerifiedAt =
            DateTime.UtcNow;


        // ACTIVE subscription-na old accidental
        // CancelledAt value preserve panna koodathu.
        if (string.Equals(
            subscription.Status,
            "ACTIVE",
            StringComparison.OrdinalIgnoreCase))
        {
            subscription.CancelledAt =
                null;
        }
    }


    // =========================================================
    // ENTITY -> DTO
    // =========================================================
    //
    // Function:
    // Database entity frontend-safe DTO-a convert pannum.
    //
    // IMPORTANT:
    // User navigation / internal values expose panna maatom.
    private static SubscriptionDto MapToDto(
        Subscription subscription)
    {
        return new SubscriptionDto
        {
            Id =
                subscription.Id,

            Provider =
                subscription.Provider,

            PayPalSubscriptionId =
                subscription.PayPalSubscriptionId,

            PayPalPlanId =
                subscription.PayPalPlanId,

            PlanName =
                subscription.PlanName,

            Status =
                subscription.Status,

            IsPremium =
                string.Equals(
                    subscription.Status,
                    "ACTIVE",
                    StringComparison.OrdinalIgnoreCase),

            StartDate =
                subscription.StartDate,

            NextBillingDate =
                subscription.NextBillingDate,

            CancelledAt =
                subscription.CancelledAt,

            LastVerifiedAt =
                subscription.LastVerifiedAt,

            CreatedAt =
                subscription.CreatedAt,

            UpdatedAt =
                subscription.UpdatedAt
        };
    }


    // =========================================================
    // BUILD BILLING REFERENCE
    // =========================================================
    //
    // Function:
    // Current PDV User + PayPal subscription bind pannum.
    //
    // Example:
    //
    // UserId = 8
    //
    // Output:
    //
    // PDV-USER-8
    //
    // Later R7.7 frontend PayPal createSubscription-la
    // custom_id-aa same value send pannum.
    private static string BuildBillingReference(
        int userId)
    {
        return $"PDV-USER-{userId}";
    }


    // =========================================================
    // GET REQUIRED CONFIG
    // =========================================================
    //
    // Function:
    // Critical PayPal configuration value read pannum.
    //
    // Missing-na server configuration error.
    private string GetRequiredConfiguration(
        string key)
    {
        string? value =
            _configuration[
                key];


        if (string.IsNullOrWhiteSpace(
            value))
        {
            throw new InvalidOperationException(
                $"{key} is not configured."
            );
        }


        return value.Trim();
    }


    // =========================================================
    // GET PLAN NAME
    // =========================================================

    private string GetPlanName()
    {
        string? planName =
            _configuration[
                "PayPal:PlanName"];


        if (string.IsNullOrWhiteSpace(
            planName))
        {
            return
                "Premium Monthly";
        }


        return planName.Trim();
    }


    // =========================================================
    // GET ENVIRONMENT
    // =========================================================

    private string GetEnvironment()
    {
        string? environment =
            _configuration[
                "PayPal:Environment"];


        if (string.Equals(
            environment,
            "Live",
            StringComparison.OrdinalIgnoreCase))
        {
            return "Live";
        }


        return "Sandbox";
    }


    // =========================================================
    // NORMALIZE STATUS
    // =========================================================
    //
    // Example:
    //
    // active
    // Active
    //
    //      ↓
    //
    // ACTIVE
    private static string NormalizeStatus(
        string? status)
    {
        if (string.IsNullOrWhiteSpace(
            status))
        {
            return string.Empty;
        }


        return status
            .Trim()
            .ToUpperInvariant();
    }
}