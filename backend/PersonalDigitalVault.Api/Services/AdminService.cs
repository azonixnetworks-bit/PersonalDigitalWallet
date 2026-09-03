using PersonalDigitalVault.Api.DTOs.Admin;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;

namespace PersonalDigitalVault.Api.Services;

public class AdminService : IAdminService
{
    // =========================================================
    // DEPENDENCIES
    // =========================================================

    private readonly IUserRepository _userRepository;

    private readonly IDocumentRepository _documentRepository;

    private readonly CurrentUserService _currentUser;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Admin operations-ku thevaiyana dependencies inject pannum.
    //
    // Input:
    // IUserRepository
    // IDocumentRepository
    // CurrentUserService
    //
    // Reason:
    // User management + safe document metadata dashboard
    // handle panna.
    //
    // Output:
    // Dependencies private variables-la store aagum.
    public AdminService(
        IUserRepository userRepository,
        IDocumentRepository documentRepository,
        CurrentUserService currentUser)
    {
        _userRepository =
            userRepository;

        _documentRepository =
            documentRepository;

        _currentUser =
            currentUser;
    }


    // =========================================================
    // ADMIN DASHBOARD
    // =========================================================

    // Function:
    // Admin dashboard-ku system summary metadata return pannum.
    //
    // Output:
    // Total users
    // Total uploads
    // Total stored document records
    // Latest upload metadata
    //
    // Security:
    // Admin-ku document content decrypt panna maatom.
    //
    // Return panna koodatha values:
    //
    // StoragePath
    // StoredFileName
    // FileHash
    // encrypted bytes
    // decrypted file bytes
    // credentials
    // encryption keys
    public async Task<AdminDashboardDto>
        GetDashboardAsync()
    {
        EnsureAdmin();


        // =====================================================
        // TOTAL USERS
        // =====================================================

        int totalUsers =
            await _userRepository
                .CountAsync();


        // =====================================================
        // TOTAL DOCUMENT RECORDS
        // =====================================================

        int totalDocuments =
            await _documentRepository
                .CountAsync();


        // =====================================================
        // SAFE DOCUMENT METADATA
        // =====================================================

        var uploadedDocuments =
            await _documentRepository
                .GetAllForAdminAsync();


        // Repository recent-first order return pannina
        // first 10 records mattum dashboard-la show pannuvom.
        //
        // IMPORTANT:
        // Entity-la sensitive properties irundhaalum
        // AdminUploadDto-ku safe fields mattum map pannrom.
        var recentUploads =
            uploadedDocuments
                .Take(10)
                .Select(
                    document =>
                        new AdminUploadDto
                        {
                            UploadedBy =
                                document.User?.FullName
                                ?? "Unknown User",

                            FileName =
                                document.OriginalFileName,

                            FileSize =
                                document.FileSize,

                            UploadedAt =
                                document.CreatedAt
                        })
                .ToList();


        return new AdminDashboardDto
        {
            TotalUsers =
                totalUsers,

            TotalUploads =
                totalDocuments,

            TotalStoredFiles =
                totalDocuments,

            RecentUploads =
                recentUploads
        };
    }


    // =========================================================
    // GET USER LIST
    // =========================================================

    // Function:
    // Admin-ku basic user account metadata list return pannum.
    //
    // Output:
    // Id
    // FullName
    // Email
    // Role
    // IsActive
    //
    // Security:
    // PasswordHash
    // EmailOtpHash
    // TotpSecretEncrypted
    // LastTotpTimeStepUsed
    //
    // edhuvum DTO-ku map panna maatom.
    public async Task<List<UserListDto>>
        GetUsersAsync()
    {
        EnsureAdmin();


        var users =
            await _userRepository
                .GetAllAsync();


        return users
            .Select(
                user =>
                    new UserListDto
                    {
                        Id =
                            user.Id,

                        FullName =
                            user.FullName,

                        Email =
                            user.Email,

                        Role =
                            user.Role,

                        IsActive =
                            user.IsActive
                    })
            .ToList();
    }


    // =========================================================
    // ENABLE / DISABLE USER
    // =========================================================

    // Function:
    // Admin normal User account enable / disable pannum.
    //
    // Input:
    // id  = target user Id
    // dto = IsActive true / false
    //
    // Output:
    // None.
    //
    // Security:
    // Admin account status inga change panna mudiyathu.
    //
    // Unknown/future role-um change panna allow panna maatom.
    public async Task UpdateUserStatusAsync(
        int id,
        UpdateUserStatusDto dto)
    {
        EnsureAdmin();


        var user =
            await _userRepository
                .GetByIdAsync(
                    id);


        if (user == null)
        {
            throw new KeyNotFoundException(
                "User not found.");
        }


        // =====================================================
        // ONLY NORMAL USER ACCOUNTS
        // =====================================================

        // Admin / unknown privileged role account status
        // indha endpoint moolama change panna koodathu.
        if (!string.Equals(
                user.Role,
                "User",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Only normal user account status can be changed.");
        }


        // true  = Enable
        // false = Disable
        user.IsActive =
            dto.IsActive;


        await _userRepository
            .UpdateAsync(
                user);
    }


    // =========================================================
    // ADMIN ROLE CHECK
    // =========================================================

    // Function:
    // Current authenticated user Admin-aa verify pannum.
    //
    // Input:
    // Current JWT Role.
    //
    // Output:
    // Admin -> continue
    // User / other role -> reject
    //
    // Security:
    // Controller role authorization accidentally remove
    // aanaalum service direct-a normal user-ku admin
    // operations allow panna koodathu.
    private void EnsureAdmin()
    {
        if (!string.Equals(
                _currentUser.Role,
                "Admin",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "Access denied.");
        }
    }
}