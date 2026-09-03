using System.Text.Json;

using PersonalDigitalVault.Api.DTOs.Subscription;

namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface IPayPalWebhookService
{
    // =========================================================
    // PROCESS PAYPAL WEBHOOK
    // =========================================================
    //
    // Function:
    //
    // PayPal headers
    //      +
    // PayPal webhook JSON
    //      ↓
    // Verify signature with PayPal
    //      ↓
    // Identify event
    //      ↓
    // If subscription lifecycle event:
    // Get authoritative subscription from PayPal
    //      ↓
    // Update local database
    //
    // Input:
    //
    // headers
    // webhookEvent
    //
    // Output:
    //
    // true
    //   -> signature valid and webhook safely handled
    //
    // false
    //   -> signature invalid / cannot verify sender
    //
    // IMPORTANT:
    // Business processing failure may throw exception.
    // Controller appo 500 return pannum.
    Task<bool> ProcessAsync(
        PayPalWebhookHeadersDto headers,
        JsonElement webhookEvent);
}