using System.Text.Json;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PersonalDigitalVault.Api.DTOs.Subscription;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;


// =========================================================
// PAYPAL WEBHOOK CONTROLLER
// =========================================================
//
// IMPORTANT:
//
// Indha endpoint user browser-kaga illa.
//
// PayPal Server
//      ↓
// POST /api/paypal/webhook
//      ↓
// PDV Server
//
// Authentication:
// JWT use panna maatom.
//
// Reason:
// PayPal-kitta namma PDV user JWT illa.
//
// Instead:
//
// PayPal digital webhook signature
//      ↓
// PayPal verification API
//
// dhaan sender authenticity prove pannum.
//
// =========================================================

[ApiController]
[Route("api/paypal")]
public class PayPalWebhookController
    : ControllerBase
{
    private readonly IPayPalWebhookService
        _payPalWebhookService;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public PayPalWebhookController(
        IPayPalWebhookService payPalWebhookService)
    {
        _payPalWebhookService =
            payPalWebhookService;
    }


    // =====================================================
    // PAYPAL WEBHOOK
    // =====================================================
    //
    // API:
    //
    // POST /api/paypal/webhook
    //
    // Access:
    //
    // Public endpoint.
    //
    // BUT:
    //
    // Every message PayPal signature verify pannapadum.
    //
    // Flow:
    //
    // PayPal
    //   ↓
    // Headers + JSON event
    //   ↓
    // Controller
    //   ↓
    // PayPalWebhookService
    //   ↓
    // Verify signature
    //   ↓
    // Process subscription lifecycle
    //
    // Output:
    //
    // 200:
    // Valid webhook handled/ignored safely.
    //
    // 401:
    // Signature invalid.
    //
    // 400:
    // Invalid JSON.
    //
    // 500:
    // Verified event but temporary processing
    // failure; PayPal may retry.
    //
    // =====================================================

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult>
        Receive()
    {
        // =================================================
        // PAYPAL HEADERS
        // =================================================

        var headers =
            new PayPalWebhookHeadersDto
            {
                AuthAlgo =
                    Request.Headers[
                        "PAYPAL-AUTH-ALGO"
                    ].ToString(),

                CertUrl =
                    Request.Headers[
                        "PAYPAL-CERT-URL"
                    ].ToString(),

                TransmissionId =
                    Request.Headers[
                        "PAYPAL-TRANSMISSION-ID"
                    ].ToString(),

                TransmissionSignature =
                    Request.Headers[
                        "PAYPAL-TRANSMISSION-SIG"
                    ].ToString(),

                TransmissionTime =
                    Request.Headers[
                        "PAYPAL-TRANSMISSION-TIME"
                    ].ToString()
            };


        JsonDocument webhookDocument;


        // =================================================
        // READ WEBHOOK JSON
        // =================================================

        try
        {
            webhookDocument =
                await JsonDocument.ParseAsync(
                    Request.Body);
        }
        catch (JsonException)
        {
            return BadRequest(
                new
                {
                    message =
                        "Invalid webhook payload."
                });
        }


        using (webhookDocument)
        {
            // RootElement clone pannuvom.
            //
            // JsonDocument dispose aana piragum
            // service safely use panna independent copy.
            JsonElement webhookEvent =
                webhookDocument
                    .RootElement
                    .Clone();


            try
            {
                bool valid =
                    await _payPalWebhookService
                        .ProcessAsync(
                            headers,
                            webhookEvent);


                // =========================================
                // SIGNATURE INVALID
                // =========================================

                if (!valid)
                {
                    return Unauthorized(
                        new
                        {
                            message =
                                "Webhook verification failed."
                        });
                }


                // =========================================
                // SUCCESS
                // =========================================
                //
                // Unsupported but verified event-um
                // safely acknowledged.
                //
                // PayPal unnecessary retry panna vendam.

                return Ok(
                    new
                    {
                        message =
                            "Webhook received."
                    });
            }
            catch (InvalidOperationException)
            {
                // =========================================
                // TEMPORARY PROCESSING FAILURE
                // =========================================
                //
                // Raw exception/secret details
                // PayPal response-la expose panna maatom.

                return StatusCode(
                    StatusCodes
                        .Status500InternalServerError,

                    new
                    {
                        message =
                            "Webhook processing could not be completed."
                    });
            }
        }
    }
}