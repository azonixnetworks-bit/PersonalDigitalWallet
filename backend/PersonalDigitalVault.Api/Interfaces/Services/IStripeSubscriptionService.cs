using PersonalDigitalVault.Api.DTOs.Subscription;

namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface IStripeSubscriptionService
{
    Task<StripeSubscriptionConfigDto> GetConfigAsync();

    Task<SubscriptionDto?> GetMineAsync();

    Task<CreateCheckoutSessionResponseDto> CreateCheckoutSessionAsync();

    Task<SubscriptionDto?> VerifyCheckoutSessionAsync(
        VerifyCheckoutSessionRequestDto request);

    Task<CreatePortalSessionResponseDto> CreatePortalSessionAsync();
}
