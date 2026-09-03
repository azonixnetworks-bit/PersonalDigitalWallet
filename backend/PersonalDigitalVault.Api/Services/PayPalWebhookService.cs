using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using PersonalDigitalVault.Api.DTOs.Subscription;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Services;

public class PayPalWebhookService
    : IPayPalWebhookService
{
    // =========================================================
    // DEPENDENCIES
    // =========================================================

    // PayPal HTTP calls.
    private readonly IHttpClientFactory
        _httpClientFactory;


    // PayPal configuration.
    private readonly IConfiguration
        _configuration;


    // Existing R7.4 PayPal service.
    //
    // Webhook body status direct trust pannaama
    // current authoritative subscription details
    // PayPal API-lendhu retrieve panna use pannuvom.
    private readonly IPayPalService
        _payPalService;


    // Local subscription database access.
    private readonly ISubscriptionRepository
        _subscriptionRepository;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================
    //
    // Input:
    //
    // IHttpClientFactory
    // IConfiguration
    // IPayPalService
    // ISubscriptionRepository
    //
    // Output:
    // Dependencies service-la store aagum.
    public PayPalWebhookService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IPayPalService payPalService,
        ISubscriptionRepository subscriptionRepository)
    {
        _httpClientFactory =
            httpClientFactory;

        _configuration =
            configuration;

        _payPalService =
            payPalService;

        _subscriptionRepository =
            subscriptionRepository;
    }


    // =========================================================
    // PROCESS WEBHOOK
    // =========================================================
    //
    // Flow:
    //
    // Incoming request
    //      ↓
    // Validate required headers
    //      ↓
    // PayPal signature verification API
    //      ↓
    // SUCCESS?
    //   ├── NO -> false
    //   ↓ YES
    // Read event_type
    //      ↓
    // Supported subscription lifecycle event?
    //      ↓
    // Read resource.id
    //      ↓
    // Local subscription exists?
    //      ↓
    // Retrieve latest real subscription from PayPal
    //      ↓
    // Verify plan/user binding
    //      ↓
    // Update local status
    //
    public async Task<bool>
        ProcessAsync(
            PayPalWebhookHeadersDto headers,
            JsonElement webhookEvent)
    {
        // =====================================================
        // VERIFY SIGNATURE FIRST
        // =====================================================
        //
        // IMPORTANT:
        //
        // event_type / resource / status ellame
        // verification success varaikum trust panna maatom.

        bool signatureValid =
            await VerifySignatureAsync(
                headers,
                webhookEvent);


        if (!signatureValid)
        {
            return false;
        }


        // =====================================================
        // READ EVENT TYPE
        // =====================================================

        string eventType =
            ReadString(
                webhookEvent,
                "event_type");


        if (string.IsNullOrWhiteSpace(
            eventType))
        {
            // Signature valid but event type missing.
            //
            // Side effect panna maatom.
            return true;
        }


        // =====================================================
        // PAYMENT FAILED EVENT
        // =====================================================
        //
        // Important:
        //
        // One failed recurring payment vandha udane
        // Premium remove panna maatom.
        //
        // PayPal retry behavior irukkum.
        // Threshold exceed aana subscription
        // eventually SUSPENDED aagalaam.
        //
        // SUSPENDED webhook vandha appo
        // authoritative status sync pannuvom.

        if (string.Equals(
            eventType,
            "BILLING.SUBSCRIPTION.PAYMENT.FAILED",
            StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }


        // =====================================================
        // ONLY SUBSCRIPTION LIFECYCLE EVENTS
        // =====================================================

        if (!IsSupportedSubscriptionEvent(
            eventType))
        {
            // Example:
            //
            // CATALOG.PRODUCT.CREATED
            // PAYMENT.SALE.COMPLETED
            //
            // Signature valid.
            // But R7.8 subscription status sync-ku
            // currently action thevai illa.
            //
            // 2xx receive panna true return.
            return true;
        }


        // =====================================================
        // READ PAYPAL RESOURCE ID
        // =====================================================
        //
        // Expected subscription lifecycle payload:
        //
        // resource
        //    ↓
        // id
        //
        // Idha local PayPalSubscriptionId-oda compare pannuvom.

        string subscriptionId =
            ReadNestedString(
                webhookEvent,
                "resource",
                "id");


        if (string.IsNullOrWhiteSpace(
            subscriptionId))
        {
            // Verified payload but usable subscription ID illa.
            //
            // Unknown data DB-la save panna maatom.
            return true;
        }


        // =====================================================
        // LOCAL SUBSCRIPTION
        // =====================================================

        Subscription? localSubscription =
            await _subscriptionRepository
                .GetByPayPalSubscriptionIdAsync(
                    subscriptionId);


        if (localSubscription == null)
        {
            // =================================================
            // WHY 200 / IGNORE?
            // =================================================
            //
            // Webhook frontend confirm-ku munnaadi
            // arrive aagalaam.
            //
            // Example:
            //
            // PayPal ACTIVATED webhook
            //      ↓
            // User browser confirm API
            //
            // Local record innum create aagalaam.
            //
            // Unknown subscription automatically
            // create pannaama safe-aa ignore pannuvom.
            //
            // Frontend confirm later authoritative
            // PayPal verification panni create pannum.

            return true;
        }


        // =====================================================
        // GET AUTHORITATIVE SUBSCRIPTION FROM PAYPAL
        // =====================================================
        //
        // IMPORTANT SECURITY:
        //
        // Webhook body:
        // status = CANCELLED
        //
        // nu direct database update panna maatom.
        //
        // Verified webhook event trigger mattum.
        //
        // Current state:
        // GET PayPal Subscription API
        //
        // moolama retrieve pannuvom.
        //
        // Idhanala duplicate/stale webhook vandhaalum
        // current PayPal state dhaan DB-ku pogum.

        PayPalSubscriptionDetailsDto? paypalSubscription =
            await _payPalService
                .GetSubscriptionAsync(
                    subscriptionId);


        if (paypalSubscription == null)
        {
            // PayPal temporary API problem irukkalaam.
            //
            // Exception throw pannina controller 500.
            // PayPal webhook retry panna chance irukkum.
            throw new InvalidOperationException(
                "PayPal subscription status could not be verified."
            );
        }


        // =====================================================
        // SUBSCRIPTION ID MATCH
        // =====================================================

        if (!string.Equals(
            paypalSubscription.SubscriptionId,
            localSubscription.PayPalSubscriptionId,
            StringComparison.Ordinal))
        {
            return true;
        }


        // =====================================================
        // VERIFY PLAN ID
        // =====================================================
        //
        // Vera PayPal plan local Premium record-ku
        // update panna koodathu.

        string expectedPlanId =
            GetRequiredConfiguration(
                "PayPal:PlanId");


        if (!string.Equals(
            paypalSubscription.PlanId,
            expectedPlanId,
            StringComparison.Ordinal))
        {
            return true;
        }


        // =====================================================
        // VERIFY USER BILLING REFERENCE
        // =====================================================
        //
        // Local:
        //
        // UserId = 8
        //
        // Expected:
        //
        // PDV-USER-8
        //
        // PayPal:
        //
        // custom_id = PDV-USER-8
        //
        // Same-a irukkanum.

        string expectedBillingReference =
            BuildBillingReference(
                localSubscription.UserId);


        if (!string.Equals(
            paypalSubscription.BillingReference,
            expectedBillingReference,
            StringComparison.Ordinal))
        {
            return true;
        }


        // =====================================================
        // UPDATE LOCAL SUBSCRIPTION
        // =====================================================

        UpdateLocalSubscription(
            localSubscription,
            paypalSubscription);


        await _subscriptionRepository
            .UpdateAsync(
                localSubscription);


        return true;
    }


    // =========================================================
    // VERIFY PAYPAL WEBHOOK SIGNATURE
    // =========================================================
    //
    // PayPal Verification API:
    //
    // POST
    // /v1/notifications/verify-webhook-signature
    //
    // Input:
    //
    // auth_algo
    // cert_url
    // transmission_id
    // transmission_sig
    // transmission_time
    // webhook_id
    // webhook_event
    //
    // Output:
    //
    // verification_status == SUCCESS
    //
    // appo mattum true.
    private async Task<bool>
        VerifySignatureAsync(
            PayPalWebhookHeadersDto headers,
            JsonElement webhookEvent)
    {
        // =====================================================
        // REQUIRED HEADER CHECK
        // =====================================================

        if (headers == null)
        {
            return false;
        }


        if (string.IsNullOrWhiteSpace(
                headers.AuthAlgo) ||
            string.IsNullOrWhiteSpace(
                headers.CertUrl) ||
            string.IsNullOrWhiteSpace(
                headers.TransmissionId) ||
            string.IsNullOrWhiteSpace(
                headers.TransmissionSignature) ||
            string.IsNullOrWhiteSpace(
                headers.TransmissionTime))
        {
            return false;
        }


        // =====================================================
        // WEBHOOK ID
        // =====================================================
        //
        // PayPal Developer Dashboard-la webhook create
        // pannumbodhu Webhook ID kidaikkum.
        //
        // Idhu User Secret/config-la irukkanum.
        //
        // Frontend-ku send panna koodathu.

        string webhookId =
            GetRequiredConfiguration(
                "PayPal:WebhookId");


        // =====================================================
        // OAUTH ACCESS TOKEN
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
            $"{GetBaseUrl()}/v1/notifications/verify-webhook-signature";


        // =====================================================
        // VERIFICATION BODY
        // =====================================================

        string jsonBody =
            JsonSerializer.Serialize(
                new
                {
                    auth_algo =
                        headers.AuthAlgo,

                    cert_url =
                        headers.CertUrl,

                    transmission_id =
                        headers.TransmissionId,

                    transmission_sig =
                        headers.TransmissionSignature,

                    transmission_time =
                        headers.TransmissionTime,

                    webhook_id =
                        webhookId,

                    webhook_event =
                        webhookEvent
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
        // CALL PAYPAL
        // =====================================================

        using HttpResponseMessage response =
            await client.SendAsync(
                request);


        if (!response.IsSuccessStatusCode)
        {
            return false;
        }


        string responseJson =
            await response.Content
                .ReadAsStringAsync();


        if (string.IsNullOrWhiteSpace(
            responseJson))
        {
            return false;
        }


        // =====================================================
        // READ verification_status
        // =====================================================

        try
        {
            using JsonDocument document =
                JsonDocument.Parse(
                    responseJson);


            string verificationStatus =
                ReadString(
                    document.RootElement,
                    "verification_status");


            return string.Equals(
                verificationStatus,
                "SUCCESS",
                StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }


    // =========================================================
    // GET PAYPAL OAUTH TOKEN
    // =========================================================
    //
    // Function:
    //
    // ClientId + ClientSecret
    //      ↓
    // PayPal OAuth endpoint
    //      ↓
    // Access Token
    //
    // NOTE:
    // R7.4 PayPalService-kullayum similar private logic irukku.
    //
    // Existing PayPalService contract change pannaama
    // R7.8 simple-aa independent verification service
    // maintain panna inga local method use pannuvom.
    private async Task<string?>
        GetAccessTokenAsync()
    {
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


        request.Content =
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    {
                        "grant_type",
                        "client_credentials"
                    }
                });


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


        try
        {
            using JsonDocument document =
                JsonDocument.Parse(
                    json);


            string accessToken =
                ReadString(
                    document.RootElement,
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
    // UPDATE LOCAL SUBSCRIPTION
    // =========================================================
    //
    // Function:
    //
    // Authoritative PayPal state
    //      ↓
    // Local Subscription entity
    //
    // update pannum.
    private static void UpdateLocalSubscription(
        Subscription subscription,
        PayPalSubscriptionDetailsDto paypal)
    {
        DateTime now =
            DateTime.UtcNow;


        string status =
            NormalizeStatus(
                paypal.Status);


        subscription.PayPalPlanId =
            paypal.PlanId;


        subscription.Status =
            status;


        subscription.StartDate =
            paypal.StartDate;


        subscription.NextBillingDate =
            paypal.NextBillingDate;


        subscription.LastVerifiedAt =
            now;


        subscription.UpdatedAt =
            now;


        // =====================================================
        // ACTIVE
        // =====================================================

        if (string.Equals(
            status,
            "ACTIVE",
            StringComparison.OrdinalIgnoreCase))
        {
            // Subscription active again-na
            // local CancelledAt clear pannuvom.
            subscription.CancelledAt =
                null;

            return;
        }


        // =====================================================
        // TERMINAL STATUS
        // =====================================================

        if (string.Equals(
                status,
                "CANCELLED",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                status,
                "EXPIRED",
                StringComparison.OrdinalIgnoreCase))
        {
            // First time terminal state observe pannumbodhu
            // local cancellation/terminal timestamp record.
            if (!subscription.CancelledAt.HasValue)
            {
                subscription.CancelledAt =
                    now;
            }


            subscription.NextBillingDate =
                null;
        }
    }


    // =========================================================
    // SUPPORTED SUBSCRIPTION EVENTS
    // =========================================================

    private static bool IsSupportedSubscriptionEvent(
        string eventType)
    {
        return
            string.Equals(
                eventType,
                "BILLING.SUBSCRIPTION.CREATED",
                StringComparison.OrdinalIgnoreCase)
            ||
            string.Equals(
                eventType,
                "BILLING.SUBSCRIPTION.ACTIVATED",
                StringComparison.OrdinalIgnoreCase)
            ||
            string.Equals(
                eventType,
                "BILLING.SUBSCRIPTION.UPDATED",
                StringComparison.OrdinalIgnoreCase)
            ||
            string.Equals(
                eventType,
                "BILLING.SUBSCRIPTION.CANCELLED",
                StringComparison.OrdinalIgnoreCase)
            ||
            string.Equals(
                eventType,
                "BILLING.SUBSCRIPTION.SUSPENDED",
                StringComparison.OrdinalIgnoreCase)
            ||
            string.Equals(
                eventType,
                "BILLING.SUBSCRIPTION.EXPIRED",
                StringComparison.OrdinalIgnoreCase);
    }


    // =========================================================
    // BILLING REFERENCE
    // =========================================================

    private static string BuildBillingReference(
        int userId)
    {
        return $"PDV-USER-{userId}";
    }


    // =========================================================
    // NORMALIZE STATUS
    // =========================================================

    private static string NormalizeStatus(
        string? status)
    {
        if (string.IsNullOrWhiteSpace(
            status))
        {
            return string.Empty;
        }


        return status
            .Trim()
            .ToUpperInvariant();
    }


    // =========================================================
    // GET REQUIRED CONFIGURATION
    // =========================================================

    private string GetRequiredConfiguration(
        string key)
    {
        string? value =
            _configuration[
                key];


        if (string.IsNullOrWhiteSpace(
            value))
        {
            throw new InvalidOperationException(
                $"{key} is not configured."
            );
        }


        return value.Trim();
    }


    // =========================================================
    // GET PAYPAL BASE URL
    // =========================================================

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
    // READ JSON STRING
    // =========================================================

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
    // READ NESTED JSON STRING
    // =========================================================
    //
    // Example:
    //
    // resource
    //     ↓
    // id
    private static string ReadNestedString(
        JsonElement root,
        string objectName,
        string propertyName)
    {
        if (!root.TryGetProperty(
                objectName,
                out JsonElement nested))
        {
            return string.Empty;
        }


        if (nested.ValueKind !=
            JsonValueKind.Object)
        {
            return string.Empty;
        }


        return ReadString(
            nested,
            propertyName);
    }
}