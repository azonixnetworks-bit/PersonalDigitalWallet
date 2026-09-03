using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using PersonalDigitalVault.Api.DTOs.Subscription;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Services;

public class PayPalService
    : IPayPalService
{
    // =========================================================
    // DEPENDENCIES
    // =========================================================

    // HttpClient factory:
    // PayPal REST API call panna use pannuvom.
    private readonly IHttpClientFactory
        _httpClientFactory;


    // Configuration:
    //
    // PayPal:
    // ClientId
    // ClientSecret
    // Environment
    //
    // values read panna.
    private readonly IConfiguration
        _configuration;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================
    //
    // Input:
    // IHttpClientFactory
    // IConfiguration
    //
    // Output:
    // Dependencies private fields-la save aagum.
    public PayPalService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory =
            httpClientFactory;

        _configuration =
            configuration;
    }


    // =========================================================
    // GET PAYPAL SUBSCRIPTION DETAILS
    // =========================================================
    //
    // Function:
    //
    // Subscription ID
    //      ↓
    // Get PayPal OAuth Access Token
    //      ↓
    // GET PayPal Subscription API
    //      ↓
    // Parse safe required fields
    //      ↓
    // Return DTO
    //
    // Input:
    // subscriptionId
    //
    // Output:
    // PayPalSubscriptionDetailsDto
    // or null.
    public async Task<PayPalSubscriptionDetailsDto?>
        GetSubscriptionAsync(
            string subscriptionId)
    {
        // =====================================================
        // INPUT VALIDATION
        // =====================================================

        string cleanSubscriptionId =
            NormalizeSubscriptionId(
                subscriptionId);


        if (string.IsNullOrWhiteSpace(
            cleanSubscriptionId))
        {
            return null;
        }


        // =====================================================
        // PAYPAL ACCESS TOKEN
        // =====================================================

        string? accessToken =
            await GetAccessTokenAsync();


        if (string.IsNullOrWhiteSpace(
            accessToken))
        {
            return null;
        }


        // =====================================================
        // CREATE HTTP CLIENT
        // =====================================================

        HttpClient client =
            _httpClientFactory
                .CreateClient();


        // =====================================================
        // PAYPAL URL
        // =====================================================

        string url =
            $"{GetBaseUrl()}/v1/billing/subscriptions/" +
            Uri.EscapeDataString(
                cleanSubscriptionId);


        // =====================================================
        // CREATE REQUEST
        // =====================================================

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                url);


        // OAuth Bearer token.
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);


        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));


        // =====================================================
        // SEND REQUEST
        // =====================================================

        using HttpResponseMessage response =
            await client.SendAsync(
                request);


        // Subscription invalid / PayPal rejected.
        //
        // Frontend-ku PayPal raw error expose panna maatom.
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }


        // =====================================================
        // READ JSON
        // =====================================================

        string json =
            await response.Content
                .ReadAsStringAsync();


        if (string.IsNullOrWhiteSpace(
            json))
        {
            return null;
        }


        // =====================================================
        // PARSE PAYPAL RESPONSE
        // =====================================================

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(
                    json);


            JsonElement root =
                document.RootElement;


            // ---------------------------------------------
            // SUBSCRIPTION ID
            // ---------------------------------------------

            string id =
                ReadString(
                    root,
                    "id");


            // ---------------------------------------------
            // PLAN ID
            // ---------------------------------------------

            string planId =
                ReadString(
                    root,
                    "plan_id");


            // ---------------------------------------------
            // STATUS
            // ---------------------------------------------

            string status =
                ReadString(
                    root,
                    "status");


            // Essential PayPal values missing-na
            // invalid response-aa treat pannuvom.
            if (string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(planId) ||
                string.IsNullOrWhiteSpace(status))
            {
                return null;
            }


            // ---------------------------------------------
            // BILLING REFERENCE / CUSTOM ID
            // ---------------------------------------------

            string? billingReference =
                ReadNullableString(
                    root,
                    "custom_id");


            // ---------------------------------------------
            // START DATE
            // ---------------------------------------------

            DateTime? startDate =
                ReadDateTime(
                    root,
                    "start_time");


            // ---------------------------------------------
            // NEXT BILLING DATE
            // ---------------------------------------------

            DateTime? nextBillingDate =
                ReadNestedDateTime(
                    root,
                    "billing_info",
                    "next_billing_time");


            // =================================================
            // RETURN SAFE DTO
            // =================================================

            return new PayPalSubscriptionDetailsDto
            {
                SubscriptionId =
                    id,

                PlanId =
                    planId,

                Status =
                    status,

                BillingReference =
                    billingReference,

                StartDate =
                    startDate,

                NextBillingDate =
                    nextBillingDate
            };
        }
        catch (JsonException)
        {
            // Invalid PayPal JSON.
            //
            // Raw response / secret/token log panna maatom.
            return null;
        }
    }


    // =========================================================
    // CANCEL SUBSCRIPTION
    // =========================================================
    //
    // Function:
    //
    // Current subscription ID
    //      ↓
    // OAuth token
    //      ↓
    // POST /cancel
    //      ↓
    // PayPal cancellation
    //
    // Output:
    // true / false.
    public async Task<bool>
        CancelSubscriptionAsync(
            string subscriptionId,
            string reason)
    {
        // =====================================================
        // VALIDATE SUBSCRIPTION ID
        // =====================================================

        string cleanSubscriptionId =
            NormalizeSubscriptionId(
                subscriptionId);


        if (string.IsNullOrWhiteSpace(
            cleanSubscriptionId))
        {
            return false;
        }


        // =====================================================
        // VALIDATE REASON
        // =====================================================
        //
        // PayPal cancel reason:
        // 1 - 128 characters.
        //
        // Empty reason vandha safe default use pannuvom.

        string cleanReason =
            string.IsNullOrWhiteSpace(reason)
                ?
                "Cancelled by the subscriber."
                :
                reason.Trim();


        if (cleanReason.Length > 128)
        {
            cleanReason =
                cleanReason.Substring(
                    0,
                    128);
        }


        // =====================================================
        // GET ACCESS TOKEN
        // =====================================================

        string? accessToken =
            await GetAccessTokenAsync();


        if (string.IsNullOrWhiteSpace(
            accessToken))
        {
            return false;
        }


        // =====================================================
        // HTTP CLIENT
        // =====================================================

        HttpClient client =
            _httpClientFactory
                .CreateClient();


        string url =
            $"{GetBaseUrl()}/v1/billing/subscriptions/" +
            Uri.EscapeDataString(
                cleanSubscriptionId) +
            "/cancel";


        // =====================================================
        // JSON BODY
        // =====================================================

        string jsonBody =
            JsonSerializer.Serialize(
                new
                {
                    reason =
                        cleanReason
                });


        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                url);


        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);


        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));


        request.Content =
            new StringContent(
                jsonBody,
                Encoding.UTF8,
                "application/json");


        // =====================================================
        // SEND CANCEL REQUEST
        // =====================================================

        using HttpResponseMessage response =
            await client.SendAsync(
                request);


        // PayPal successful cancellation normally
        // 204 No Content return pannum.
        return response.IsSuccessStatusCode;
    }


    // =========================================================
    // GET PAYPAL OAUTH ACCESS TOKEN
    // =========================================================
    //
    // Function:
    //
    // ClientId + ClientSecret
    //      ↓
    // Basic Authentication
    //      ↓
    // POST /v1/oauth2/token
    //      ↓
    // access_token
    //
    // IMPORTANT:
    //
    // ClientSecret frontend-ku pogathu.
    //
    // AccessToken database/localStorage-la save panna maatom.
    //
    // Output:
    // PayPal OAuth access token
    // or null.
    private async Task<string?>
        GetAccessTokenAsync()
    {
        // =====================================================
        // READ CONFIGURATION
        // =====================================================

        string? clientId =
            _configuration[
                "PayPal:ClientId"];


        string? clientSecret =
            _configuration[
                "PayPal:ClientSecret"];


        if (string.IsNullOrWhiteSpace(
                clientId) ||
            string.IsNullOrWhiteSpace(
                clientSecret))
        {
            return null;
        }


        // =====================================================
        // BASIC AUTH VALUE
        // =====================================================
        //
        // Format:
        //
        // ClientId:ClientSecret
        //       ↓
        // Base64
        //
        // Header:
        // Authorization: Basic XXXXX

        string credentials =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    $"{clientId}:{clientSecret}"));


        HttpClient client =
            _httpClientFactory
                .CreateClient();


        string url =
            $"{GetBaseUrl()}/v1/oauth2/token";


        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                url);


        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Basic",
                credentials);


        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));


        // =====================================================
        // OAUTH BODY
        // =====================================================
        //
        // grant_type=client_credentials

        request.Content =
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    {
                        "grant_type",
                        "client_credentials"
                    }
                });


        // =====================================================
        // SEND TOKEN REQUEST
        // =====================================================

        using HttpResponseMessage response =
            await client.SendAsync(
                request);


        if (!response.IsSuccessStatusCode)
        {
            return null;
        }


        string json =
            await response.Content
                .ReadAsStringAsync();


        if (string.IsNullOrWhiteSpace(
            json))
        {
            return null;
        }


        // =====================================================
        // READ ACCESS TOKEN
        // =====================================================

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(
                    json);


            JsonElement root =
                document.RootElement;


            string accessToken =
                ReadString(
                    root,
                    "access_token");


            if (string.IsNullOrWhiteSpace(
                accessToken))
            {
                return null;
            }


            return accessToken;
        }
        catch (JsonException)
        {
            return null;
        }
    }


    // =========================================================
    // GET PAYPAL BASE URL
    // =========================================================
    //
    // Function:
    // Sandbox / Live environment decide pannum.
    //
    // Configuration:
    //
    // PayPal:Environment = Sandbox
    //
    // Development-ku sandbox.
    //
    // Live explicit-aa configure pannina mattum
    // production URL use pannuvom.
    private string GetBaseUrl()
    {
        string environment =
            _configuration[
                "PayPal:Environment"]
            ??
            "Sandbox";


        if (string.Equals(
            environment,
            "Live",
            StringComparison.OrdinalIgnoreCase))
        {
            return
                "https://api-m.paypal.com";
        }


        return
            "https://api-m.sandbox.paypal.com";
    }


    // =========================================================
    // NORMALIZE SUBSCRIPTION ID
    // =========================================================
    //
    // Function:
    // PayPal subscription ID basic validation.
    //
    // Input:
    // raw ID.
    //
    // Output:
    // trimmed valid ID
    // or empty string.
    private static string NormalizeSubscriptionId(
        string? subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(
            subscriptionId))
        {
            return string.Empty;
        }


        string value =
            subscriptionId.Trim();


        // PayPal subscription ID response field
        // reasonable supported length range.
        if (value.Length < 3 ||
            value.Length > 50)
        {
            return string.Empty;
        }


        // URL/path input-la whitespace allow panna maatom.
        if (value.Any(
            char.IsWhiteSpace))
        {
            return string.Empty;
        }


        return value;
    }


    // =========================================================
    // READ REQUIRED STRING
    // =========================================================
    //
    // Function:
    // JSON property safe-aa string read pannum.
    //
    // Invalid/missing-na empty string.
    private static string ReadString(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out JsonElement property))
        {
            return string.Empty;
        }


        if (property.ValueKind !=
            JsonValueKind.String)
        {
            return string.Empty;
        }


        return property
            .GetString()?
            .Trim()
            ??
            string.Empty;
    }


    // =========================================================
    // READ OPTIONAL STRING
    // =========================================================

    private static string? ReadNullableString(
        JsonElement element,
        string propertyName)
    {
        string value =
            ReadString(
                element,
                propertyName);


        if (string.IsNullOrWhiteSpace(
            value))
        {
            return null;
        }


        return value;
    }


    // =========================================================
    // READ DATE TIME
    // =========================================================
    //
    // Function:
    // PayPal UTC/Internet date value safe-aa parse pannum.
    private static DateTime? ReadDateTime(
        JsonElement element,
        string propertyName)
    {
        string value =
            ReadString(
                element,
                propertyName);


        if (string.IsNullOrWhiteSpace(
            value))
        {
            return null;
        }


        if (DateTimeOffset.TryParse(
            value,
            out DateTimeOffset date))
        {
            return date.UtcDateTime;
        }


        return null;
    }


    // =========================================================
    // READ NESTED DATE
    // =========================================================
    //
    // Example:
    //
    // billing_info
    //      ↓
    // next_billing_time
    private static DateTime? ReadNestedDateTime(
        JsonElement root,
        string objectName,
        string propertyName)
    {
        if (!root.TryGetProperty(
                objectName,
                out JsonElement nested))
        {
            return null;
        }


        if (nested.ValueKind !=
            JsonValueKind.Object)
        {
            return null;
        }


        return ReadDateTime(
            nested,
            propertyName);
    }
}