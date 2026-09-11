using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using PersonalDigitalVault.Api.DTOs.Subscription;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Services;

public class StripeService : IStripeService
{
    private const string StripeApiBaseUrl = "https://api.stripe.com/v1/";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;

    public StripeService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<StripeService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> CreateCustomerAsync(User user)
    {
        var form = new Dictionary<string, string>
        {
            ["email"] = user.Email,
            ["name"] = user.FullName,
            ["metadata[pdv_user_id]"] = user.Id.ToString(CultureInfo.InvariantCulture)
        };

        // The key only deduplicates retries of this logical request. The database
        // remains the long-term source of truth for the Stripe customer binding.
        string idempotencyKey = $"pdv-customer-user-{user.Id}";

        using JsonDocument document = await PostFormAsync(
            "customers",
            form,
            idempotencyKey);

        string? customerId = GetString(document.RootElement, "id");

        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new InvalidOperationException("Stripe customer creation did not return an id.");
        }

        return customerId;
    }

    public async Task<StripePriceDetailsDto?> GetConfiguredPriceAsync()
    {
        string priceId = GetRequiredConfiguration("Stripe:PriceId");

        using JsonDocument? document = await GetJsonOrNullAsync(
            $"prices/{Uri.EscapeDataString(priceId)}");

        if (document == null)
        {
            return null;
        }

        return ParsePrice(document.RootElement);
    }

    public async Task<StripeCheckoutSessionDetailsDto> CreateCheckoutSessionAsync(
        User user,
        string stripeCustomerId)
    {
        StripePriceDetailsDto price = await GetValidatedConfiguredPriceAsync();
       string frontendBaseUrl = GetFrontendBaseUrl();

string successUrl =
    $"{frontendBaseUrl}/subscription?checkout=success&session_id={{CHECKOUT_SESSION_ID}}";

string cancelUrl =
    $"{frontendBaseUrl}/subscription?checkout=cancelled";

        var form = new Dictionary<string, string>
        {
            ["mode"] = "subscription",
            ["customer"] = stripeCustomerId,
            ["client_reference_id"] = user.Id.ToString(CultureInfo.InvariantCulture),
            ["line_items[0][price]"] = price.PriceId,
            ["line_items[0][quantity]"] = "1",
            ["success_url"] = successUrl,
            ["cancel_url"] = cancelUrl,
            ["metadata[pdv_user_id]"] = user.Id.ToString(CultureInfo.InvariantCulture),
            ["subscription_data[metadata][pdv_user_id]"] = user.Id.ToString(CultureInfo.InvariantCulture),
            ["billing_address_collection"] = "auto"
        };

        string idempotencyKey = $"pdv-checkout-{user.Id}-{Guid.NewGuid():N}";

        using JsonDocument document = await PostFormAsync(
            "checkout/sessions",
            form,
            idempotencyKey);

        StripeCheckoutSessionDetailsDto session = ParseCheckoutSession(document.RootElement);

        if (string.IsNullOrWhiteSpace(session.SessionId) ||
            string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe Checkout Session creation failed.");
        }

        if (!IsLiveModeExpected(session.LiveMode))
        {
            throw new InvalidOperationException("Stripe Checkout environment does not match application configuration.");
        }

        return session;
    }

    public async Task<StripeCheckoutSessionDetailsDto?> GetCheckoutSessionAsync(
        string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        using JsonDocument? document = await GetJsonOrNullAsync(
            $"checkout/sessions/{Uri.EscapeDataString(sessionId.Trim())}");

        if (document == null)
        {
            return null;
        }

        StripeCheckoutSessionDetailsDto session = ParseCheckoutSession(document.RootElement);

        return IsLiveModeExpected(session.LiveMode)
            ? session
            : null;
    }

    public async Task<StripeSubscriptionDetailsDto?> GetSubscriptionAsync(
        string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return null;
        }

        using JsonDocument? document = await GetJsonOrNullAsync(
            $"subscriptions/{Uri.EscapeDataString(subscriptionId.Trim())}");

        if (document == null)
        {
            return null;
        }

        StripeSubscriptionDetailsDto subscription = ParseSubscription(document.RootElement);

        return IsLiveModeExpected(subscription.LiveMode)
            ? subscription
            : null;
    }

    public async Task<string> CreatePortalSessionAsync(string stripeCustomerId)
    {
        if (string.IsNullOrWhiteSpace(stripeCustomerId))
        {
            throw new InvalidOperationException("Stripe customer id is required.");
        }

        //string baseUrl = GetRequiredConfiguration("App:BaseUrl").TrimEnd('/');
        string frontendBaseUrl = GetFrontendBaseUrl();

        var form = new Dictionary<string, string>
        {
            ["customer"] = stripeCustomerId,
            ["return_url"] = $"{frontendBaseUrl}/subscription"
        };

        string idempotencyKey = $"pdv-portal-{Guid.NewGuid():N}";

        using JsonDocument document = await PostFormAsync(
            "billing_portal/sessions",
            form,
            idempotencyKey);

        string? url = GetString(document.RootElement, "url");

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Stripe Customer Portal did not return a URL.");
        }

        return url;
    }

    private async Task<StripePriceDetailsDto> GetValidatedConfiguredPriceAsync()
    {
        StripePriceDetailsDto? price = await GetConfiguredPriceAsync();

        if (price == null)
        {
            throw new InvalidOperationException("Configured Stripe Price was not found.");
        }

        if (!price.Active)
        {
            throw new InvalidOperationException("Configured Stripe Price is inactive.");
        }

        if (!price.IsRecurring)
        {
            throw new InvalidOperationException("Configured Stripe Price must be recurring.");
        }

        string? expectedProductId = _configuration["Stripe:ProductId"]?.Trim();

        if (!string.IsNullOrWhiteSpace(expectedProductId) &&
            !string.Equals(price.ProductId, expectedProductId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Configured Stripe Price does not belong to the expected Product.");
        }

        return price;
    }

    private async Task<JsonDocument> PostFormAsync(
        string relativePath,
        IReadOnlyDictionary<string, string> values,
        string? idempotencyKey = null)
    {
        using var request = CreateRequest(HttpMethod.Post, relativePath);
        request.Content = new FormUrlEncodedContent(values);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        }

        using HttpResponseMessage response = await CreateClient().SendAsync(request);
        return await ReadJsonResponseAsync(response, relativePath);
    }

    private async Task<JsonDocument?> GetJsonOrNullAsync(string relativePath)
    {
        using var request = CreateRequest(HttpMethod.Get, relativePath);
        using HttpResponseMessage response = await CreateClient().SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadJsonResponseAsync(response, relativePath);
    }

    private string GetFrontendBaseUrl()
{
    string? frontendBaseUrl =
        _configuration["App:FrontendBaseUrl"]?.Trim();

    if (string.IsNullOrWhiteSpace(frontendBaseUrl))
    {
        frontendBaseUrl =
            GetRequiredConfiguration("App:BaseUrl");
    }

    return frontendBaseUrl.TrimEnd('/');
}

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        string secretKey = GetRequiredConfiguration("Stripe:SecretKey");

        var request = new HttpRequestMessage(
            method,
            StripeApiBaseUrl + relativePath.TrimStart('/'));

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", secretKey);

        return request;
    }

    private HttpClient CreateClient()
    {
        HttpClient client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    private async Task<JsonDocument> ReadJsonResponseAsync(
        HttpResponseMessage response,
        string relativePath)
    {
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            string? stripeRequestId = null;

            if (response.Headers.TryGetValues("Request-Id", out var requestIds))
            {
                stripeRequestId = requestIds.FirstOrDefault();
            }

            _logger.LogWarning(
                "Stripe API request {Path} failed with HTTP {StatusCode}. StripeRequestId={StripeRequestId}",
                relativePath,
                (int)response.StatusCode,
                stripeRequestId ?? "n/a");

            throw new InvalidOperationException("Stripe API request failed.");
        }

        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Stripe API returned invalid JSON for {Path}.", relativePath);
            throw new InvalidOperationException("Stripe API returned an invalid JSON response.");
        }
    }

    private static StripePriceDetailsDto ParsePrice(JsonElement element)
    {
        string interval = string.Empty;
        long intervalCount = 1;

        if (element.TryGetProperty("recurring", out JsonElement recurring) &&
            recurring.ValueKind == JsonValueKind.Object)
        {
            interval = GetString(recurring, "interval") ?? string.Empty;

            if (recurring.TryGetProperty("interval_count", out JsonElement intervalCountElement) &&
                intervalCountElement.TryGetInt64(out long parsedIntervalCount) &&
                parsedIntervalCount > 0)
            {
                intervalCount = parsedIntervalCount;
            }
        }

        long? unitAmount = null;

        if (element.TryGetProperty("unit_amount", out JsonElement amountElement) &&
            amountElement.ValueKind == JsonValueKind.Number &&
            amountElement.TryGetInt64(out long parsedAmount))
        {
            unitAmount = parsedAmount;
        }

        return new StripePriceDetailsDto
        {
            PriceId = GetString(element, "id") ?? string.Empty,
            ProductId = GetExpandableId(element, "product") ?? string.Empty,
            Active = GetBoolean(element, "active"),
            Currency = (GetString(element, "currency") ?? string.Empty).ToUpperInvariant(),
            UnitAmount = unitAmount,
            RecurringInterval = interval.ToUpperInvariant(),
            RecurringIntervalCount = intervalCount
        };
    }

    private static StripeCheckoutSessionDetailsDto ParseCheckoutSession(JsonElement element)
    {
        return new StripeCheckoutSessionDetailsDto
        {
            SessionId = GetString(element, "id") ?? string.Empty,
            Url = GetString(element, "url"),
            CustomerId = GetExpandableId(element, "customer"),
            SubscriptionId = GetExpandableId(element, "subscription"),
            ClientReferenceId = GetString(element, "client_reference_id"),
            Status = NormalizeStatus(GetString(element, "status")),
            PaymentStatus = NormalizeStatus(GetString(element, "payment_status")),
            LiveMode = GetBoolean(element, "livemode")
        };
    }

    internal static StripeSubscriptionDetailsDto ParseSubscription(JsonElement element)
    {
        string priceId = string.Empty;
        DateTime? nextBillingDate = null;

        if (element.TryGetProperty("items", out JsonElement items) &&
            items.ValueKind == JsonValueKind.Object &&
            items.TryGetProperty("data", out JsonElement data) &&
            data.ValueKind == JsonValueKind.Array &&
            data.GetArrayLength() > 0)
        {
            JsonElement firstItem = data[0];

            nextBillingDate =
                GetUnixDateTime(firstItem, "current_period_end") ??
                GetUnixDateTime(element, "current_period_end");

            if (firstItem.TryGetProperty("price", out JsonElement price))
            {
                priceId = GetExpandableIdValue(price) ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(priceId) &&
                firstItem.TryGetProperty("plan", out JsonElement plan))
            {
                priceId = GetExpandableIdValue(plan) ?? string.Empty;
            }
        }
        else
        {
            nextBillingDate = GetUnixDateTime(element, "current_period_end");
        }

        int? metadataUserId = null;

        if (element.TryGetProperty("metadata", out JsonElement metadata) &&
            metadata.ValueKind == JsonValueKind.Object &&
            metadata.TryGetProperty("pdv_user_id", out JsonElement userIdElement))
        {
            string? userIdText = userIdElement.ValueKind == JsonValueKind.String
                ? userIdElement.GetString()
                : userIdElement.ToString();

            if (int.TryParse(
                userIdText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int parsedUserId) &&
                parsedUserId > 0)
            {
                metadataUserId = parsedUserId;
            }
        }

        return new StripeSubscriptionDetailsDto
        {
            SubscriptionId = GetString(element, "id") ?? string.Empty,
            CustomerId = GetExpandableId(element, "customer") ?? string.Empty,
            PriceId = priceId,
            Status = NormalizeStatus(GetString(element, "status")),
            MetadataUserId = metadataUserId,
            StartDate =
                GetUnixDateTime(element, "start_date") ??
                GetUnixDateTime(element, "created"),
            NextBillingDate = nextBillingDate,
            CancelAtPeriodEnd = GetBoolean(element, "cancel_at_period_end"),
            CancelledAt = GetUnixDateTime(element, "canceled_at"),
            LiveMode = GetBoolean(element, "livemode")
        };
    }

    private bool IsLiveModeExpected(bool liveMode)
    {
        string environment = _configuration["Stripe:Environment"]?.Trim() ?? "Sandbox";
        bool expectLive = string.Equals(environment, "Live", StringComparison.OrdinalIgnoreCase);
        return liveMode == expectLive;
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

        return GetExpandableIdValue(value);
    }

    private static string? GetExpandableIdValue(JsonElement value)
    {
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

    private static DateTime? GetUnixDateTime(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement value) ||
            value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        long unixSeconds;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out unixSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
        }

        if (value.ValueKind == JsonValueKind.String &&
            long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out unixSeconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
        }

        return null;
    }

    private string GetRequiredConfiguration(string key)
    {
        string? value = _configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required Stripe configuration '{key}' is missing.");
        }

        return value.Trim();
    }

    private static string NormalizeStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "UNKNOWN"
            : status.Trim().ToUpperInvariant();
    }
}
