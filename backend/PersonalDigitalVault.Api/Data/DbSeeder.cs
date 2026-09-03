using Microsoft.EntityFrameworkCore;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Security;

namespace PersonalDigitalVault.Api.Data;

public class DbSeeder
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public DbSeeder(
        AppDbContext context,
        PasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    // INPUT:
    // Direct input illa.
    // Admin details configuration-lendhu edukkum.
    //
    // REASON:
    // Project first run-la default Admin account create panna.
    //
    // OUTPUT:
    // Admin already irundha onnum create pannaadhu.
    // Admin illaina secure password hash panni create pannum.
    public async Task SeedAsync()
    {
        var adminEmail =
            _configuration["SeedAdmin:Email"]
            ?? "admin@pdv.local";

        var adminFullName =
            _configuration["SeedAdmin:FullName"]
            ?? "System Administrator";

        var adminPassword =
            _configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                "SeedAdmin password User Secrets-la configure pannunga.");
        }

        // Same admin already database-la irukka nu check.
        var existingAdmin =
            await _context.Users
                .FirstOrDefaultAsync(
                    x => x.Email == adminEmail);

        // Already irundha duplicate create panna koodathu.
        if (existingAdmin != null)
        {
            return;
        }

        var admin = new User
        {
            FullName = adminFullName,

            Email = adminEmail,

            Role = "Admin",

            IsActive = true,

            CreatedAt = DateTime.UtcNow
        };

        // Plain admin password database-la save panna maatom.
        admin.PasswordHash =
            _passwordHasher.HashPassword(
                admin,
                adminPassword);

        _context.Users.Add(admin);

        await _context.SaveChangesAsync();
    }
}