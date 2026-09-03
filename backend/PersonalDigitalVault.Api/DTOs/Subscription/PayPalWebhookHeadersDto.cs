namespace PersonalDigitalVault.Api.DTOs.Subscription;

public class PayPalWebhookHeadersDto
{
    // =========================================================
    // PAYPAL AUTH ALGORITHM
    // =========================================================
    //
    // PayPal webhook request header:
    //
    // PAYPAL-AUTH-ALGO
    //
    // Function:
    // PayPal signature verify panna use aagum.
    public string AuthAlgo { get; set; }
        = string.Empty;


    // =========================================================
    // PAYPAL CERTIFICATE URL
    // =========================================================
    //
    // Header:
    //
    // PAYPAL-CERT-URL
    //
    // PayPal public certificate location.
    public string CertUrl { get; set; }
        = string.Empty;


    // =========================================================
    // TRANSMISSION ID
    // =========================================================
    //
    // Header:
    //
    // PAYPAL-TRANSMISSION-ID
    //
    // Each webhook transmission-ku unique identifier.
    public string TransmissionId { get; set; }
        = string.Empty;


    // =========================================================
    // TRANSMISSION SIGNATURE
    // =========================================================
    //
    // Header:
    //
    // PAYPAL-TRANSMISSION-SIG
    //
    // PayPal generate pannina digital signature.
    public string TransmissionSignature { get; set; }
        = string.Empty;


    // =========================================================
    // TRANSMISSION TIME
    // =========================================================
    //
    // Header:
    //
    // PAYPAL-TRANSMISSION-TIME
    //
    // Webhook transmission timestamp.
    public string TransmissionTime { get; set; }
        = string.Empty;
}