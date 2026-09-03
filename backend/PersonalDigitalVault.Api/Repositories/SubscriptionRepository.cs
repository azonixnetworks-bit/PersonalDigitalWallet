using Microsoft.EntityFrameworkCore;
using PersonalDigitalVault.Api.Data;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Repositories;

namespace PersonalDigitalVault.Api.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _context;

    public SubscriptionRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Subscription?> GetLatestByUserIdAsync(int userId)
    {
        return _context.Subscriptions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public Task<Subscription?> GetLatestByUserIdAndProviderAsync(
        int userId,
        string provider)
    {
        return _context.Subscriptions
            .Where(x => x.UserId == userId && x.Provider == provider)
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public Task<List<Subscription>> GetByUserIdAsync(int userId)
    {
        return _context.Subscriptions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public Task<Subscription?> GetByPayPalSubscriptionIdAsync(
        string paypalSubscriptionId)
    {
        return _context.Subscriptions
            .FirstOrDefaultAsync(
                x => x.PayPalSubscriptionId == paypalSubscriptionId);
    }

    public Task<Subscription?> GetByStripeSubscriptionIdAsync(
        string stripeSubscriptionId)
    {
        return _context.Subscriptions
            .FirstOrDefaultAsync(
                x => x.StripeSubscriptionId == stripeSubscriptionId);
    }

    public Task<List<Subscription>> GetAllForAdminAsync()
    {
        return _context.Subscriptions
            .Include(x => x.User)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Subscription subscription)
    {
        await _context.Subscriptions.AddAsync(subscription);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Subscription subscription)
    {
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync();
    }
}
