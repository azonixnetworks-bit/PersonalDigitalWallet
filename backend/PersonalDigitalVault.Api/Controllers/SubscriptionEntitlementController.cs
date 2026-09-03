using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PersonalDigitalVault.Api.DTOs.Subscription;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;


// =========================================================
// SUBSCRIPTION ENTITLEMENT CONTROLLER
// =========================================================
//
// Function:
//
// Frontend-ku:
//
// Current plan
// Storage usage
// Document usage
// Limits
//
// return pannum.
//
// Security:
//
// Normal authenticated User mattum.
// Admin-ku private vault entitlement endpoint thevai illa.
// =========================================================

[ApiController]
[Route("api/subscriptions/entitlements")]
[Authorize(Roles = "User")]
public class SubscriptionEntitlementController
    : ControllerBase
{
    private readonly
        ISubscriptionEntitlementService
        _entitlementService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public SubscriptionEntitlementController(
        ISubscriptionEntitlementService entitlementService)
    {
        _entitlementService =
            entitlementService;
    }


    // =====================================================
    // GET CURRENT PLAN LIMITS
    // =====================================================
    //
    // API:
    //
    // GET /api/subscriptions/entitlements
    //
    // Input:
    // JWT only.
    //
    // Output:
    //
    // {
    //   planName,
    //   isPremium,
    //   maxDocuments,
    //   currentDocuments,
    //   storageLimitBytes,
    //   storageUsedBytes,
    //   storageRemainingBytes,
    //   maxUploadBytes,
    //   canUpload
    // }
    [HttpGet]
    public async Task<ActionResult<
        SubscriptionEntitlementDto>>
        GetCurrent()
    {
        SubscriptionEntitlementDto result =
            await _entitlementService
                .GetCurrentAsync();


        return Ok(
            result);
    }
}