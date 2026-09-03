namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface IStripeWebhookService
{
    Task<bool> ProcessAsync(string rawPayload, string stripeSignatureHeader);
}
