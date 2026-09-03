using PersonalDigitalVault.Api.Entities;

namespace PersonalDigitalVault.Api.Interfaces.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetLatestByUserIdAsync(int userId);

    Task<Subscription?> GetLatestByUserIdAndProviderAsync(
        int userId,
        string provider);

    Task<List<Subscription>> GetByUserIdAsync(int userId);

    Task<Subscription?> GetByPayPalSubscriptionIdAsync(
        string paypalSubscriptionId);

    Task<Subscription?> GetByStripeSubscriptionIdAsync(
        string stripeSubscriptionId);

    Task<List<Subscription>> GetAllForAdminAsync();

    Task AddAsync(Subscription subscription);

    Task UpdateAsync(Subscription subscription);
}
