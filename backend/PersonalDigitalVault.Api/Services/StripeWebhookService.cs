using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using PersonalDigitalVault.Api.DTOs.Subscription;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Services;

public class StripeWebhookService : IStripeWebhookService
{
    private readonly IStripeService _stripeService;
    private readonly StripeSubscriptionSynchronizer _synchronizer;
    private readonly IConfiguration _configuration;

    public StripeWebhookService(
        IStripeService stripeService,
        StripeSubscriptionSynchronizer synchronizer,
        IConfiguration configuration)
    {
        _stripeService = stripeService;
        _synchronizer = synchronizer;
        _configuration = configuration;
    }

    public async Task<bool> ProcessAsync(
        string rawPayload,
        string stripeSignatureHeader)
    {
        if (!VerifySignature(rawPayload, stripeSignatureHeader))
        {
            return false;
        }

        using JsonDocument document = JsonDocument.Parse(rawPayload);
        JsonElement root = document.RootElement;

        bool eventLiveMode = GetBoolean(root, "livemode");
        string environment = _configuration["Stripe:Environment"]?.Trim() ?? "Sandbox";
        bool expectLive = string.Equals(environment, "Live", StringComparison.OrdinalIgnoreCase);

        // A valid signature is not enough if the event belongs to the wrong
        // Stripe mode. Test events must never mutate a Live billing database
        // and Live events must never mutate a Sandbox database.
        if (eventLiveMode != expectLive)
        {
            return false;
        }

        string eventType = GetString(root, "type") ?? string.Empty;

        if (!root.TryGetProperty("data", out JsonElement data) ||
            !data.TryGetProperty("object", out JsonElement dataObject))
        {
            return true;
        }

        switch (eventType)
        {
            case "checkout.session.completed":
            case "checkout.session.async_payment_succeeded":
                await HandleCheckoutCompletedAsync(dataObject);
                break;

            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
            case "customer.subscription.paused":
            case "customer.subscription.resumed":
                await HandleSubscriptionSnapshotAsync(dataObject);
                break;

            case "invoice.paid":
            case "invoice.payment_succeeded":
            case "invoice.payment_failed":
            case "invoice.payment_action_required":
                await HandleInvoiceAsync(dataObject);
                break;

            // Verified but irrelevant events are acknowledged safely.
            default:
                break;
        }

        return true;
    }

    private async Task HandleCheckoutCompletedAsync(JsonElement checkoutObject)
    {
        string? sessionId = GetString(checkoutObject, "id");

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        StripeCheckoutSessionDetailsDto? session =
            await _stripeService.GetCheckoutSessionAsync(sessionId);

        if (session == null || string.IsNullOrWhiteSpace(session.SubscriptionId))
        {
            return;
        }

        StripeSubscriptionDetailsDto? subscription =
            await _stripeService.GetSubscriptionAsync(session.SubscriptionId);

        if (subscription == null)
        {
            return;
        }

        int? expectedUserId = null;

        if (int.TryParse(
            session.ClientReferenceId,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out int userId) &&
            userId > 0)
        {
            expectedUserId = userId;
        }

        await _synchronizer.SyncAsync(subscription, expectedUserId);
    }

    private async Task HandleSubscriptionSnapshotAsync(JsonElement subscriptionObject)
    {
        StripeSubscriptionDetailsDto subscription =
            StripeService.ParseSubscription(subscriptionObject);

        await _synchronizer.SyncAsync(subscription);
    }

    private async Task HandleInvoiceAsync(JsonElement invoiceObject)
    {
        string? subscriptionId = ExtractInvoiceSubscriptionId(invoiceObject);

        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return;
        }

        StripeSubscriptionDetailsDto? subscription =
            await _stripeService.GetSubscriptionAsync(subscriptionId);

        if (subscription != null)
        {
            await _synchronizer.SyncAsync(subscription);
        }
    }

    private bool VerifySignature(string payload, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(payload) ||
            string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        string webhookSecret = _configuration["Stripe:WebhookSecret"]?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new InvalidOperationException("Stripe webhook secret is not configured.");
        }

        long? timestamp = null;
        var signatures = new List<string>();

        foreach (string part in signatureHeader.Split(','))
        {
            string[] pieces = part.Split('=', 2);

            if (pieces.Length != 2)
            {
                continue;
            }

            string key = pieces[0].Trim();
            string value = pieces[1].Trim();

            if (key == "t" &&
                long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedTimestamp))
            {
                timestamp = parsedTimestamp;
            }
            else if (key == "v1" && !string.IsNullOrWhiteSpace(value))
            {
                signatures.Add(value);
            }
        }

        if (!timestamp.HasValue || signatures.Count == 0)
        {
            return false;
        }

        int toleranceSeconds = 300;

        if (int.TryParse(
            _configuration["Stripe:WebhookToleranceSeconds"],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int configuredTolerance) &&
            configuredTolerance >= 0)
        {
            toleranceSeconds = configuredTolerance;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (Math.Abs(now - timestamp.Value) > toleranceSeconds)
        {
            return false;
        }

        string signedPayload = $"{timestamp.Value}.{payload}";
        byte[] secretBytes = Encoding.UTF8.GetBytes(webhookSecret);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(signedPayload);

        byte[] expectedHash;

        using (var hmac = new HMACSHA256(secretBytes))
        {
            expectedHash = hmac.ComputeHash(payloadBytes);
        }

        foreach (string signature in signatures)
        {
            try
            {
                byte[] receivedHash = Convert.FromHexString(signature);

                if (receivedHash.Length == expectedHash.Length &&
                    CryptographicOperations.FixedTimeEquals(receivedHash, expectedHash))
                {
                    return true;
                }
            }
            catch (FormatException)
            {
                // Ignore malformed signature values and continue checking others.
            }
        }

        return false;
    }

    private static string? ExtractInvoiceSubscriptionId(JsonElement invoiceObject)
    {
        string? direct = GetExpandableId(invoiceObject, "subscription");

        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        // Newer Stripe invoice payloads can expose the subscription beneath
        // parent.subscription_details.subscription.
        if (invoiceObject.TryGetProperty("parent", out JsonElement parent) &&
            parent.ValueKind == JsonValueKind.Object &&
            parent.TryGetProperty("subscription_details", out JsonElement details) &&
            details.ValueKind == JsonValueKind.Object)
        {
            return GetExpandableId(details, "subscription");
        }

        return null;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value) ||
            value.ValueKind == JsonValueKind.Null ||
            value.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.ToString();
    }

    private static string? GetExpandableId(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            return GetString(value, "id");
        }

        return null;
    }

    private static bool GetBoolean(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value))
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        return value.ValueKind == JsonValueKind.String &&
               bool.TryParse(value.GetString(), out bool parsed) &&
               parsed;
    }
}
