using Microsoft.EntityFrameworkCore;
using PersonalDigitalVault.Api.Data;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Repositories;

namespace PersonalDigitalVault.Api.Repositories;

public class UserRepository(AppDbContext db)
    : IUserRepository
{
    // =========================================================
    // GET USER BY EMAIL
    // =========================================================
    //
    // Function:
    // Email base panni user find pannum.
    //
    // Input:
    // email
    //
    // Output:
    // User object or null.
    public Task<User?> GetByEmailAsync(
        string email)
    {
        return db.Users
            .FirstOrDefaultAsync(
                x => x.Email == email);
    }


    // =========================================================
    // GET USER BY ID
    // =========================================================
    //
    // Function:
    // User Id base panni user find pannum.
    //
    // Input:
    // id
    //
    // Output:
    // User object or null.
    public Task<User?> GetByIdAsync(
        int id)
    {
        return db.Users
            .FirstOrDefaultAsync(
                x => x.Id == id);
    }


    // =========================================================
    // GET USER BY STRIPE CUSTOMER ID
    // =========================================================
    public Task<User?> GetByStripeCustomerIdAsync(
        string stripeCustomerId)
    {
        return db.Users
            .FirstOrDefaultAsync(
                x => x.StripeCustomerId == stripeCustomerId);
    }


    // =========================================================
    // GET USER BY PASSWORD RESET TOKEN HASH
    // =========================================================
    //
    // Function:
    // Password reset token hash base panni
    // matching user-a find pannum.
    //
    // Input:
    // tokenHash
    //
    // Example:
    //
    // Browser token:
    // abc123....
    //
    //       ↓ SHA-256
    //
    // 9FC31A....
    //
    //       ↓
    //
    // PasswordResetTokenHash == 9FC31A....
    //
    //       ↓
    //
    // Correct user
    //
    // Security:
    // Raw reset token database-la store/search
    // panna maatom.
    //
    // Output:
    // Matching user -> User
    // Match illa   -> null
    public Task<User?> GetByPasswordResetTokenHashAsync(
        string tokenHash)
    {
        return db.Users
            .FirstOrDefaultAsync(
                x =>
                    x.PasswordResetTokenHash ==
                    tokenHash);
    }


    // =========================================================
    // GET ALL USERS
    // =========================================================
    //
    // Function:
    // Admin page-ku users ellam return pannum.
    //
    // AsNoTracking:
    // Read-only operation.
    // Entity changes track panna thevai illa.
    public Task<List<User>> GetAllAsync()
    {
        return db.Users
            .AsNoTracking()
            .OrderByDescending(
                x => x.CreatedAt)
            .ToListAsync();
    }


    // =========================================================
    // COUNT USERS
    // =========================================================
    //
    // Function:
    // Admin dashboard total users count.
    public Task<int> CountAsync()
    {
        return db.Users.CountAsync();
    }


    // =========================================================
    // ADD USER
    // =========================================================
    //
    // Function:
    // Registration time-la new user
    // database-la save pannum.
    //
    // Input:
    // User entity.
    //
    // Output:
    // Database insert complete.
    public async Task AddAsync(
        User user)
    {
        db.Users.Add(user);

        await db.SaveChangesAsync();
    }


    // =========================================================
    // UPDATE USER
    // =========================================================
    //
    // Function:
    // Existing user data update pannum.
    //
    // Forgot Password-la use:
    //
    // 1. ResetTokenHash save
    // 2. Expiry save
    // 3. LastSentAt save
    // 4. New PasswordHash save
    // 5. Reset fields clear
    //
    // Output:
    // Changes database-la save aagum.
    public async Task UpdateAsync(
        User user)
    {
        db.Users.Update(user);

        await db.SaveChangesAsync();
    }


    // =========================================================
    // SAVE CHANGES
    // =========================================================
    //
    // Function:
    // Pending database changes save pannum.
    public async Task SaveChangesAsync()
    {
        await db.SaveChangesAsync();
    }
}