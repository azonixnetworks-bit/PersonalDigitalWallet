//using System.Security.Cryptography;
//namespace PersonalDigitalVault.Api.Security;
//public class PasswordHasher
//{
//    public string Hash(string password){byte[] salt=RandomNumberGenerator.GetBytes(16);byte[] hash=Rfc2898DeriveBytes.Pbkdf2(password,salt,100000,HashAlgorithmName.SHA256,32);return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";}
//    public bool Verify(string password,string stored){var p=stored.Split('.');if(p.Length!=2)return false;var salt=Convert.FromBase64String(p[0]);var expected=Convert.FromBase64String(p[1]);var actual=Rfc2898DeriveBytes.Pbkdf2(password,salt,100000,HashAlgorithmName.SHA256,32);return CryptographicOperations.FixedTimeEquals(actual,expected);}
//}


using Microsoft.AspNetCore.Identity;
using PersonalDigitalVault.Api.Entities;

namespace PersonalDigitalVault.Api.Security;

public class PasswordHasher
{
    // ASP.NET Core built-in secure password hasher
    private readonly PasswordHasher<User> _passwordHasher = new();

    // INPUT:
    // user     -> password belong aagura user
    // password -> user/admin enter pannura plain password
    //
    // REASON:
    // Plain password database-la save panna koodathu.
    //
    // OUTPUT:
    // Secure hashed password return pannum.
    public string HashPassword(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    // INPUT:
    // user           -> database user
    // hashedPassword -> database-la save panna password hash
    // enteredPassword-> login-la user enter panna password
    //
    // REASON:
    // Login password correct-a nu verify panna.
    //
    // OUTPUT:
    // correct -> true
    // wrong   -> false
    public bool VerifyPassword(
        User user,
        string hashedPassword,
        string enteredPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            user,
            hashedPassword,
            enteredPassword);

        return result != PasswordVerificationResult.Failed;
    }
}