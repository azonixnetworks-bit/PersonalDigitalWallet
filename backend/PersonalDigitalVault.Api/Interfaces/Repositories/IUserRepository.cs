using PersonalDigitalVault.Api.Entities;

namespace PersonalDigitalVault.Api.Interfaces.Repositories;

public interface IUserRepository
{
    // =========================================================
    // GET USER BY EMAIL
    // =========================================================
    //
    // Function:
    // Email address base panni user find pannum.
    //
    // Input:
    // email
    //
    // Used by:
    // Login
    // Registration checks
    // Forgot Password
    //
    // Output:
    // User found -> User object
    // User illa  -> null
    Task<User?> GetByEmailAsync(string email);


    // =========================================================
    // GET USER BY ID
    // =========================================================
    //
    // Function:
    // User Id base panni user find pannum.
    //
    // Output:
    // User object or null
    Task<User?> GetByIdAsync(int id);


    Task<User?> GetByStripeCustomerIdAsync(string stripeCustomerId);


    // =========================================================
    // GET USER BY PASSWORD RESET TOKEN HASH
    // =========================================================
    //
    // Function:
    // Password reset token hash base panni
    // correct user-a database-la find pannum.
    //
    // Input:
    // SHA-256 token hash.
    //
    // Important:
    // Raw reset token database-ku search panna maatom.
    //
    // Browser raw token
    //      ↓
    // PasswordResetTokenService.HashToken()
    //      ↓
    // SHA-256 Hash
    //      ↓
    // Intha method
    //      ↓
    // User find
    //
    // Output:
    // Matching user -> User object
    // Invalid token -> null
    Task<User?> GetByPasswordResetTokenHashAsync(
        string tokenHash);


    // =========================================================
    // GET ALL USERS
    // =========================================================
    //
    // Admin safe user list-ku use pannum.
    Task<List<User>> GetAllAsync();


    // =========================================================
    // COUNT USERS
    // =========================================================
    //
    // Admin dashboard total users count.
    Task<int> CountAsync();


    // =========================================================
    // ADD USER
    // =========================================================
    //
    // Registration-la new user save pannum.
    Task AddAsync(User user);


    // =========================================================
    // UPDATE USER
    // =========================================================
    //
    // Existing user changes database-la save pannum.
    //
    // Forgot Password flow-la:
    // - reset hash save
    // - expiry save
    // - password change
    // - reset fields clear
    //
    // ellathukkum use aagum.
    Task UpdateAsync(User user);


    // =========================================================
    // SAVE CHANGES
    // =========================================================
    Task SaveChangesAsync();
}