using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;

[ApiController]
[Route("api/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IStripeWebhookService _stripeWebhookService;

    public StripeWebhookController(IStripeWebhookService stripeWebhookService)
    {
        _stripeWebhookService = stripeWebhookService;
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Receive()
    {
        string payload;

        using (var reader = new StreamReader(Request.Body))
        {
            payload = await reader.ReadToEndAsync();
        }

        string signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        try
        {
            bool valid = await _stripeWebhookService.ProcessAsync(payload, signatureHeader);

            if (!valid)
            {
                return Unauthorized(new { message = "Webhook verification failed." });
            }

            return Ok(new { message = "Webhook received." });
        }
        catch (System.Text.Json.JsonException)
        {
            return BadRequest(new { message = "Invalid webhook payload." });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Webhook processing could not be completed." });
        }
    }
}
