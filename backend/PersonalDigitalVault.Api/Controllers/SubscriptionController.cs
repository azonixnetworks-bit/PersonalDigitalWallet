using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PersonalDigitalVault.Api.DTOs.Subscription;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;


// =========================================================
// SUBSCRIPTION CONTROLLER
// =========================================================
//
// PURPOSE:
//
// Current normal User-ku PayPal subscription APIs
// expose panna indha controller use pannuvom.
//
// FLOW:
//
// Frontend
//    ↓
// SubscriptionController
//    ↓
// ISubscriptionService
//    ↓
// SubscriptionService
//    ↓
// PayPalService + SubscriptionRepository
//
//
// SECURITY:
//
// ✅ Final JWT required
// ✅ Role = User mattum
// ✅ Admin-ku access illa
// ✅ Browser UserId accept panna maatom
// ✅ ClientSecret return panna maatom
// ✅ PayPal approval result direct trust panna maatom
//
// =========================================================

[ApiController]
[Route("api/subscriptions")]
[Authorize(Roles = "User")]
public class SubscriptionController
    : ControllerBase
{
    // =====================================================
    // SUBSCRIPTION SERVICE
    // =====================================================

    private readonly ISubscriptionService
        _subscriptionService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================
    //
    // Function:
    // Subscription business logic service inject pannum.
    //
    // Input:
    // ISubscriptionService
    //
    // Why:
    // Controller direct database / PayPal API access
    // panna koodathu.
    //
    // Output:
    // _subscriptionService-la save aagum.
    public SubscriptionController(
        ISubscriptionService subscriptionService)
    {
        _subscriptionService =
            subscriptionService;
    }


    // =====================================================
    // 1. GET PAYPAL CONFIG
    // =====================================================
    //
    // API:
    //
    // GET /api/subscriptions/config
    //
    // INPUT:
    //
    // Request body illa.
    //
    // User identity:
    // Final JWT-lendhu service edukkum.
    //
    // OUTPUT:
    //
    // {
    //     clientId,
    //     planId,
    //     planName,
    //     environment,
    //     billingReference
    // }
    //
    // IMPORTANT SECURITY:
    //
    // ❌ ClientSecret return panna koodathu.
    // ❌ WebhookId return panna koodathu.
    // ❌ PayPal OAuth token return panna koodathu.
    //
    // =====================================================

    [HttpGet("config")]
    public async Task<IActionResult>
        GetConfig()
    {
        try
        {
            SubscriptionConfigDto config =
                await _subscriptionService
                    .GetConfigAsync();


            return Ok(
                config);
        }
        catch (UnauthorizedAccessException)
        {
            // Disabled / invalid / incomplete
            // normal user account.
            //
            // Internal reason frontend-ku expose panna maatom.
            return Unauthorized(
                new
                {
                    message =
                        "Your account is not authorized to use subscriptions."
                });
        }
    }


    // =====================================================
    // 2. GET MY SUBSCRIPTION
    // =====================================================
    //
    // API:
    //
    // GET /api/subscriptions/me
    //
    // FUNCTION:
    // Current authenticated User-oda
    // latest subscription return pannum.
    //
    // INPUT:
    // None.
    //
    // OUTPUT — subscription irundha:
    //
    // {
    //     hasSubscription: true,
    //     subscription: { ... }
    // }
    //
    // Subscription illaina:
    //
    // {
    //     hasSubscription: false,
    //     subscription: null
    // }
    //
    // Why 404 return pannaama 200?
    //
    // Subscription illa-nradhu normal application state.
    // Frontend easy-aa Free Plan display panna mudiyum.
    //
    // =====================================================

    [HttpGet("me")]
    public async Task<IActionResult>
        GetMine()
    {
        try
        {
            SubscriptionDto? subscription =
                await _subscriptionService
                    .GetMineAsync();


            // =============================================
            // NO SUBSCRIPTION
            // =============================================

            if (subscription == null)
            {
                return Ok(
                    new
                    {
                        hasSubscription =
                            false,

                        subscription =
                            (SubscriptionDto?)null
                    });
            }


            // =============================================
            // SUBSCRIPTION EXISTS
            // =============================================

            return Ok(
                new
                {
                    hasSubscription =
                        true,

                    subscription
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Your account is not authorized to use subscriptions."
                });
        }
    }


    // =====================================================
    // 3. CONFIRM PAYPAL SUBSCRIPTION
    // =====================================================
    //
    // API:
    //
    // POST /api/subscriptions/confirm
    //
    // INPUT:
    //
    // {
    //     "subscriptionId": "I-XXXXXXXX"
    // }
    //
    // FLOW:
    //
    // PayPal frontend approval
    //        ↓
    // Subscription ID
    //        ↓
    // Controller
    //        ↓
    // SubscriptionService
    //        ↓
    // PayPal REST API
    //        ↓
    // Verify:
    //
    // SubscriptionId
    // PlanId
    // BillingReference
    // Status ACTIVE
    // Ownership
    //        ↓
    // DB Save
    //
    // IMPORTANT:
    //
    // Controller frontend success message-ai
    // direct trust pannaathu.
    //
    // =====================================================

    [HttpPost("confirm")]
    public async Task<IActionResult>
        Confirm(
            [FromBody]
            ConfirmSubscriptionRequestDto request)
    {
        try
        {
            // =============================================
            // BASIC REQUEST VALIDATION
            // =============================================
            //
            // [ApiController] + DataAnnotations already
            // validation handle pannum.
            //
            // Additional empty check readability-ku.

            if (request == null ||
                string.IsNullOrWhiteSpace(
                    request.SubscriptionId))
            {
                return BadRequest(
                    new
                    {
                        message =
                            "A valid PayPal subscription ID is required."
                    });
            }


            // =============================================
            // VERIFY + SAVE
            // =============================================

            SubscriptionDto? subscription =
                await _subscriptionService
                    .ConfirmAsync(
                        request);


            // =============================================
            // VERIFICATION FAILED
            // =============================================
            //
            // Exact reason expose panna maatom:
            //
            // wrong plan?
            // foreign subscription?
            // inactive subscription?
            // invalid PayPal ID?
            //
            // Generic response security-ku better.

            if (subscription == null)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "The PayPal subscription could not be verified."
                    });
            }


            // =============================================
            // SUCCESS
            // =============================================

            return Ok(
                new
                {
                    message =
                        "Premium subscription activated successfully.",

                    subscription
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Your account is not authorized to use subscriptions."
                });
        }
    }


    // =====================================================
    // 4. CANCEL MY SUBSCRIPTION
    // =====================================================
    //
    // API:
    //
    // POST /api/subscriptions/cancel
    //
    // INPUT:
    // None.
    //
    // IMPORTANT SECURITY:
    //
    // Frontend subscription ID send panna vendam.
    //
    // Backend:
    //
    // Current JWT UserId
    //      ↓
    // Own latest subscription
    //      ↓
    // PayPalSubscriptionId
    //      ↓
    // PayPal cancel API
    //
    // Idhanala another user's subscription ID guess panni
    // cancel panna mudiyathu.
    //
    // OUTPUT:
    //
    // 200 -> cancellation success
    // 400 -> no cancellable subscription / PayPal failed
    //
    // =====================================================

    [HttpPost("cancel")]
    public async Task<IActionResult>
        Cancel()
    {
        try
        {
            bool success =
                await _subscriptionService
                    .CancelAsync();


            // =============================================
            // CANCEL FAILED
            // =============================================

            if (!success)
            {
                return BadRequest(
                    new
                    {
                        message =
                            "The subscription could not be cancelled."
                    });
            }


            // =============================================
            // SUCCESS
            // =============================================

            return Ok(
                new
                {
                    message =
                        "Subscription cancelled successfully."
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Your account is not authorized to use subscriptions."
                });
        }
    }
}