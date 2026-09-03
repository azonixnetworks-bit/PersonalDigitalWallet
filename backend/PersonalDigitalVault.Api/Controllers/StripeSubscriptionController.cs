using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalDigitalVault.Api.DTOs.Subscription;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;

[ApiController]
[Route("api/stripe/subscriptions")]
[Authorize(Roles = "User")]
public class StripeSubscriptionController : ControllerBase
{
    private readonly IStripeSubscriptionService _subscriptionService;

    public StripeSubscriptionController(
        IStripeSubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        try
        {
            return Ok(await _subscriptionService.GetConfigAsync());
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new
            {
                message = "Your account is not authorized to use subscriptions."
            });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = "Stripe subscription configuration is incomplete."
                });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        try
        {
            SubscriptionDto? subscription =
                await _subscriptionService.GetMineAsync();

            return Ok(new
            {
                hasSubscription = subscription != null,
                subscription
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new
            {
                message = "Your account is not authorized to use subscriptions."
            });
        }
    }

    [HttpPost("checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession()
    {
        try
        {
            return Ok(
                await _subscriptionService
                    .CreateCheckoutSessionAsync());
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new
            {
                message = "Your account is not authorized to use subscriptions."
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("verify-checkout")]
    public async Task<IActionResult> VerifyCheckout(
        [FromBody] VerifyCheckoutSessionRequestDto request)
    {
        try
        {
            SubscriptionDto? subscription =
                await _subscriptionService
                    .VerifyCheckoutSessionAsync(request);

            if (subscription == null)
            {
                return BadRequest(new
                {
                    message =
                        "The Stripe Checkout Session could not be verified."
                });
            }

            return Ok(new
            {
                message = subscription.IsPremium
                    ? "Premium subscription activated successfully."
                    : "Stripe subscription was synchronized.",
                subscription
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new
            {
                message = "Your account is not authorized to use subscriptions."
            });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = "Stripe verification is temporarily unavailable."
                });
        }
    }

    [HttpPost("portal-session")]
    public async Task<IActionResult> CreatePortalSession()
    {
        try
        {
            return Ok(
                await _subscriptionService
                    .CreatePortalSessionAsync());
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new
            {
                message = "Your account is not authorized to use subscriptions."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
