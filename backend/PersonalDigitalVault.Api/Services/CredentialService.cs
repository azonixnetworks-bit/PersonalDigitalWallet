using PersonalDigitalVault.Api.DTOs.Credential;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;

namespace PersonalDigitalVault.Api.Services;

public class CredentialService : ICredentialService
{
    // =========================================================
    // DEPENDENCIES
    // =========================================================

    private readonly ICredentialRepository _repository;

    private readonly CurrentUserService _currentUser;

    private readonly AesEncryption _aes;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Credential repository + current user +
    // AES encryption service inject pannum.
    //
    // Input:
    // ICredentialRepository
    // CurrentUserService
    // AesEncryption
    //
    // Reason:
    // Credential CRUD + ownership +
    // encryption/decryption handle panna.
    //
    // Output:
    // Dependencies private variables-la store aagum.
    public CredentialService(
        ICredentialRepository repository,
        CurrentUserService currentUser,
        AesEncryption aes)
    {
        _repository =
            repository;

        _currentUser =
            currentUser;

        _aes =
            aes;
    }


    // =========================================================
    // MAP ENTITY -> DTO
    // =========================================================

    // Function:
    // Encrypted Credential entity-a
    // frontend DTO-a convert pannum.
    //
    // Input:
    // Credential entity
    // revealPassword
    //
    // Output:
    // CredentialDto
    //
    // Security:
    // Password default-a masked.
    //
    // Single credential details endpoint mattum
    // revealPassword = true use pannum.
    private CredentialDto Map(
        Credential credential,
        bool revealPassword = false)
    {
        return new CredentialDto
        {
            Id =
                credential.Id,

            Title =
                credential.Title,

            Username =
                _aes.DecryptString(
                    credential.UsernameEncrypted),

            Password =
                revealPassword
                    ? _aes.DecryptString(
                        credential.PasswordEncrypted)
                    : "••••••••",

            Website =
                credential.Website,

            Notes =
                string.IsNullOrEmpty(
                    credential.NotesEncrypted)
                    ? null
                    : _aes.DecryptString(
                        credential.NotesEncrypted)
        };
    }


    // =========================================================
    // CREATE CREDENTIAL
    // =========================================================

    // Function:
    // Current normal user's vault-la
    // encrypted credential create pannum.
    //
    // Input:
    // Title
    // Username
    // Password
    // Website
    // Notes
    //
    // Output:
    // Created CredentialDto.
    //
    // Security:
    // UserId request DTO-lendhu edukka maatom.
    //
    // JWT current user Id mattum owner.
    public async Task<CredentialDto> CreateAsync(
        CreateCredentialDto dto)
    {
        EnsureVaultUser();


        var credential =
            new Credential
            {
                Title =
                    dto.Title.Trim(),

                UsernameEncrypted =
                    _aes.EncryptString(
                        dto.Username),

                PasswordEncrypted =
                    _aes.EncryptString(
                        dto.Password),

                Website =
                    dto.Website,

                NotesEncrypted =
                    string.IsNullOrWhiteSpace(
                        dto.Notes)
                        ? null
                        : _aes.EncryptString(
                            dto.Notes),

                // IMPORTANT:
                // Owner request body-lendhu varala.
                // Authenticated user Id mattum.
                UserId =
                    _currentUser.UserId
            };


        await _repository
            .AddAsync(
                credential);


        return Map(
            credential);
    }


    // =========================================================
    // GET MY CREDENTIALS
    // =========================================================

    // Function:
    // Current user own credentials mattum return pannum.
    //
    // Output:
    // Credential list.
    //
    // Security:
    // Repository userId filter use pannum.
    //
    // Password list-la reveal panna maatom.
    public async Task<List<CredentialDto>>
        GetAllAsync()
    {
        EnsureVaultUser();


        var credentials =
            await _repository
                .GetByUserAsync(
                    _currentUser.UserId);


        return credentials
            .Select(
                credential =>
                    Map(
                        credential))
            .ToList();
    }


    // =========================================================
    // GET SINGLE CREDENTIAL
    // =========================================================

    // Function:
    // Current user own credential details return pannum.
    //
    // Input:
    // Credential Id.
    //
    // Output:
    // CredentialDto with revealed password.
    //
    // Security:
    // GetOwnedAsync:
    //
    // CredentialId
    // +
    // Current UserId
    //
    // rendu match aana mattum record return.
    public async Task<CredentialDto> GetAsync(
        int id)
    {
        EnsureVaultUser();


        var credential =
            await _repository
                .GetOwnedAsync(
                    id,
                    _currentUser.UserId);


        if (credential == null)
        {
            // Cross-user credential irundhaalum
            // existence reveal panna maatom.
            throw new KeyNotFoundException(
                "Credential not found.");
        }


        return Map(
            credential,
            revealPassword: true);
    }


    // =========================================================
    // UPDATE CREDENTIAL
    // =========================================================

    // Function:
    // Current owner credential update pannum.
    //
    // Input:
    // Credential Id
    // UpdateCredentialDto
    //
    // Output:
    // Updated CredentialDto.
    //
    // Security:
    // User A -> User B credential update panna mudiyathu.
    public async Task<CredentialDto> UpdateAsync(
        int id,
        UpdateCredentialDto dto)
    {
        EnsureVaultUser();


        var credential =
            await _repository
                .GetOwnedAsync(
                    id,
                    _currentUser.UserId);


        if (credential == null)
        {
            throw new KeyNotFoundException(
                "Credential not found.");
        }


        credential.Title =
            dto.Title.Trim();


        credential.UsernameEncrypted =
            _aes.EncryptString(
                dto.Username);


        credential.PasswordEncrypted =
            _aes.EncryptString(
                dto.Password);


        credential.Website =
            dto.Website;


        credential.NotesEncrypted =
            string.IsNullOrWhiteSpace(
                dto.Notes)
                ? null
                : _aes.EncryptString(
                    dto.Notes);


        credential.UpdatedAt =
            DateTime.UtcNow;


        await _repository
            .UpdateAsync(
                credential);


        // Update response-la password default-a masked.
        return Map(
            credential);
    }


    // =========================================================
    // DELETE CREDENTIAL
    // =========================================================

    // Function:
    // Current owner credential delete pannum.
    //
    // Input:
    // Credential Id.
    //
    // Output:
    // None.
    //
    // Security:
    // Other user's credential delete panna mudiyathu.
    public async Task DeleteAsync(
        int id)
    {
        EnsureVaultUser();


        var credential =
            await _repository
                .GetOwnedAsync(
                    id,
                    _currentUser.UserId);


        if (credential == null)
        {
            throw new KeyNotFoundException(
                "Credential not found.");
        }


        await _repository
            .DeleteAsync(
                credential);
    }


    // =========================================================
    // VAULT ROLE CHECK
    // =========================================================

    // Function:
    // Credential vault normal User role-ku mattum
    // available-aa irukka ensure pannum.
    //
    // Input:
    // Current JWT Role claim.
    //
    // Output:
    // Role = User:
    // continue.
    //
    // Role != User:
    // access reject.
    //
    // Security:
    // Admin credential encryption/decryption module
    // use panna koodathu.
    //
    // Controller role authorization-ku idhu
    // defense-in-depth additional check.
    private void EnsureVaultUser()
    {
        if (!string.Equals(
                _currentUser.Role,
                "User",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "Access denied.");
        }
    }
}