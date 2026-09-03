using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;


// =========================================================
// ADMIN SUBSCRIPTION CONTROLLER
// =========================================================
//
// IMPORTANT:
//
// READ ONLY.
//
// Admin:
// ✅ Billing summary
// ✅ Safe subscription metadata
//
// Admin:
// ❌ Cancel
// ❌ Activate
// ❌ Modify subscription
// ❌ Access PayPal secrets
// =========================================================

[ApiController]
[Route("api/admin/subscriptions")]
[Authorize(Roles = "Admin")]
public class AdminSubscriptionController
    : ControllerBase
{
    private readonly IAdminSubscriptionService
        _adminSubscriptionService;


    public AdminSubscriptionController(
        IAdminSubscriptionService adminSubscriptionService)
    {
        _adminSubscriptionService =
            adminSubscriptionService;
    }


    // =====================================================
    // GET SUMMARY
    // =====================================================
    //
    // GET:
    //
    // /api/admin/subscriptions/summary
    [HttpGet("summary")]
    public async Task<IActionResult>
        GetSummary()
    {
        var result =
            await _adminSubscriptionService
                .GetSummaryAsync();


        return Ok(
            result);
    }


    // =====================================================
    // GET SAFE SUBSCRIPTION METADATA
    // =====================================================
    //
    // GET:
    //
    // /api/admin/subscriptions
    [HttpGet]
    public async Task<IActionResult>
        GetAll()
    {
        var result =
            await _adminSubscriptionService
                .GetAllAsync();


        return Ok(
            result);
    }
}