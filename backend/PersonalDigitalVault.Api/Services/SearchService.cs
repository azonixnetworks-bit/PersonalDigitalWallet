using PersonalDigitalVault.Api.DTOs.Search;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;

namespace PersonalDigitalVault.Api.Services;

public class SearchService : ISearchService
{
    // =========================================================
    // DEPENDENCIES
    // =========================================================

    private readonly IFolderRepository _folderRepository;

    private readonly IDocumentRepository _documentRepository;

    private readonly ICredentialRepository _credentialRepository;

    private readonly CurrentUserService _currentUser;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Search-ku thevaiyana repositories + current user helper
    // inject pannum.
    //
    // Input:
    // Folder repository
    // Document repository
    // Credential repository
    // CurrentUserService
    //
    // Output:
    // Dependencies private variables-la store aagum.
    public SearchService(
        IFolderRepository folderRepository,
        IDocumentRepository documentRepository,
        ICredentialRepository credentialRepository,
        CurrentUserService currentUser)
    {
        _folderRepository =
            folderRepository;

        _documentRepository =
            documentRepository;

        _credentialRepository =
            credentialRepository;

        _currentUser =
            currentUser;
    }


    // =========================================================
    // SEARCH MY VAULT
    // =========================================================

    // Function:
    // Current normal user's own vault metadata search pannum.
    //
    // Input:
    // keyword
    //
    // Search Areas:
    // Folder name
    // Document original filename
    // Credential title
    //
    // Output:
    // SearchResultDto list.
    //
    // Security:
    // Current UserId filter mandatory.
    //
    // Admin private vault search use panna koodathu.
    //
    // Credential username/password/notes decrypt panna maatom.
    public async Task<List<SearchResultDto>> SearchAsync(
        string keyword)
    {
        // =====================================================
        // ROLE CHECK
        // =====================================================

        EnsureVaultUser();


        // =====================================================
        // KEYWORD NORMALIZE
        // =====================================================

        string cleanKeyword =
            (keyword ?? string.Empty)
                .Trim();


        // Empty search:
        //
        // Contains("") use pannina ellaa records-um
        // return aagum.
        //
        // Adha avoid panna empty list return pannuvom.
        if (string.IsNullOrWhiteSpace(
                cleanKeyword))
        {
            return new List<SearchResultDto>();
        }


        // Current authenticated user Id once eduthukkrom.
        int userId =
            _currentUser.UserId;


        var results =
            new List<SearchResultDto>();


        // =====================================================
        // SEARCH FOLDERS
        // =====================================================

        // SECURITY:
        // Current user's folders mattum repository-lendhu
        // edukkrom.
        var folders =
            await _folderRepository
                .GetByUserAsync(
                    userId);


        results.AddRange(
            folders
                .Where(
                    folder =>
                        folder.Name.Contains(
                            cleanKeyword,
                            StringComparison.OrdinalIgnoreCase))
                .Select(
                    folder =>
                        new SearchResultDto
                        {
                            Type =
                                "Folder",

                            Id =
                                folder.Id,

                            Title =
                                folder.Name
                        })
        );


        // =====================================================
        // SEARCH DOCUMENTS
        // =====================================================

        // SECURITY:
        // Current user's OWN documents mattum.
        //
        // Shared-with-me documents inga automatically
        // include panna maatom.
        //
        // Shared document access separate sharing module-la
        // handle aagum.
        var documents =
            await _documentRepository
                .GetByUserAsync(
                    userId);


        results.AddRange(
            documents
                .Where(
                    document =>
                        document.OriginalFileName.Contains(
                            cleanKeyword,
                            StringComparison.OrdinalIgnoreCase))
                .Select(
                    document =>
                        new SearchResultDto
                        {
                            Type =
                                "Document",

                            Id =
                                document.Id,

                            Title =
                                document.OriginalFileName
                        })
        );


        // =====================================================
        // SEARCH CREDENTIALS
        // =====================================================

        // SECURITY:
        // Current user's credentials mattum.
        //
        // Title metadata mattum search pannrom.
        //
        // Username / Password / Notes decrypt panna maatom.
        var credentials =
            await _credentialRepository
                .GetByUserAsync(
                    userId);


        results.AddRange(
            credentials
                .Where(
                    credential =>
                        credential.Title.Contains(
                            cleanKeyword,
                            StringComparison.OrdinalIgnoreCase))
                .Select(
                    credential =>
                        new SearchResultDto
                        {
                            Type =
                                "Credential",

                            Id =
                                credential.Id,

                            Title =
                                credential.Title
                        })
        );


        // =====================================================
        // RETURN RESULTS
        // =====================================================

        return results;
    }


    // =========================================================
    // VAULT ROLE CHECK
    // =========================================================

    // Function:
    // Private vault search normal User role-ku mattum
    // allow pannum.
    //
    // Input:
    // Current JWT Role.
    //
    // Output:
    // User -> continue
    // Admin / other role -> reject
    //
    // Security:
    // Admin private folders/documents/credentials search
    // panna koodathu.
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