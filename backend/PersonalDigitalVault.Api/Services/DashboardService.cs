using PersonalDigitalVault.Api.DTOs.Dashboard;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;

namespace PersonalDigitalVault.Api.Services;

public class DashboardService : IDashboardService
{
    private readonly IUserRepository _userRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IDocumentShareRepository _documentShareRepository;
    private readonly ISubscriptionEntitlementService _subscriptionEntitlementService;
    private readonly CurrentUserService _currentUser;

    public DashboardService(
        IUserRepository userRepository,
        IFolderRepository folderRepository,
        IDocumentRepository documentRepository,
        ICredentialRepository credentialRepository,
        IDocumentShareRepository documentShareRepository,
        ISubscriptionEntitlementService subscriptionEntitlementService,
        CurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _folderRepository = folderRepository;
        _documentRepository = documentRepository;
        _credentialRepository = credentialRepository;
        _documentShareRepository = documentShareRepository;
        _subscriptionEntitlementService = subscriptionEntitlementService;
        _currentUser = currentUser;
    }

    public async Task<DashboardDto> GetAsync()
    {
        if (!string.Equals(_currentUser.Role, "User", StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("User dashboard access is not allowed.");
        }

        int userId = _currentUser.UserId;

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        // Keep queries sequential because all repositories share one scoped EF DbContext.
        var folders = await _folderRepository.GetByUserAsync(userId);
        var documents = await _documentRepository.GetByUserAsync(userId);
        var credentials = await _credentialRepository.GetByUserAsync(userId);
        var sharedWithMe = await _documentShareRepository.GetSharedWithMeAsync(userId);
        var entitlement = await _subscriptionEntitlementService.GetCurrentAsync();

        double usagePercent = entitlement.StorageLimitBytes <= 0
            ? 0
            : Math.Round(
                (double)entitlement.StorageUsedBytes / entitlement.StorageLimitBytes * 100,
                1);

        usagePercent = Math.Clamp(usagePercent, 0, 100);

        bool totpReady =
            user.IsTotpEnabled &&
            !string.IsNullOrWhiteSpace(user.TotpSecretEncrypted);

        string overallSecurityStatus =
            user.IsEmailVerified && totpReady
                ? "SECURE"
                : "ACTION_REQUIRED";

        string primaryActionUrl;
        string primaryActionText;
        string? secondaryActionUrl;
        string? secondaryActionText;

        if (entitlement.IsPremium &&
            string.Equals(entitlement.Provider, "PayPal", StringComparison.OrdinalIgnoreCase))
        {
            primaryActionUrl = "/html/subscription.html";
            primaryActionText = "Manage PayPal Billing";
            secondaryActionUrl = null;
            secondaryActionText = null;
        }
        else if (entitlement.IsPremium &&
                 string.Equals(entitlement.Provider, "Stripe", StringComparison.OrdinalIgnoreCase))
        {
            primaryActionUrl = "/html/stripe-subscription.html";
            primaryActionText = "Manage Stripe Billing";
            secondaryActionUrl = null;
            secondaryActionText = null;
        }
        else
        {
            primaryActionUrl = "/html/stripe-subscription.html";
            primaryActionText = "Upgrade with Stripe";
            secondaryActionUrl = "/html/subscription.html";
            secondaryActionText = "PayPal option";
        }

        var recentDocuments = documents
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new DashboardRecentDocumentDto
            {
                Id = x.Id,
                FileName = x.OriginalFileName,
                FolderName = x.Folder?.Name ?? "Root Vault",
                FileSize = x.FileSize,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        var recentFolders = folders
            .OrderByDescending(x => x.CreatedAt)
            .Take(4)
            .Select(folder => new DashboardFolderDto
            {
                Id = folder.Id,
                Name = folder.Name,
                DocumentCount = documents.Count(document => document.FolderId == folder.Id),
                CreatedAt = folder.CreatedAt
            })
            .ToList();

        return new DashboardDto
        {
            User = new DashboardUserDto
            {
                FullName = user.FullName
            },
            Summary = new DashboardSummaryDto
            {
                FolderCount = folders.Count,
                DocumentCount = documents.Count,
                CredentialCount = credentials.Count,
                SharedWithMeCount = sharedWithMe.Count
            },
            Storage = new DashboardStorageDto
            {
                UsedBytes = entitlement.StorageUsedBytes,
                LimitBytes = entitlement.StorageLimitBytes,
                RemainingBytes = entitlement.StorageRemainingBytes,
                UsagePercent = usagePercent,
                CurrentDocuments = entitlement.CurrentDocuments,
                MaxDocuments = entitlement.MaxDocuments,
                CanUpload = entitlement.CanUpload
            },
            Subscription = new DashboardPlanDto
            {
                PlanName = entitlement.PlanName,
                IsPremium = entitlement.IsPremium,
                Provider = entitlement.Provider,
                Status = entitlement.Status,
                NextBillingDate = entitlement.NextBillingDate,
                CancelAtPeriodEnd = entitlement.CancelAtPeriodEnd,
                PrimaryActionUrl = primaryActionUrl,
                PrimaryActionText = primaryActionText,
                SecondaryActionUrl = secondaryActionUrl,
                SecondaryActionText = secondaryActionText
            },
            Security = new DashboardSecurityDto
            {
                EmailVerified = user.IsEmailVerified,
                TotpEnabled = totpReady,
                EncryptedStorageActive = true,
                OverallStatus = overallSecurityStatus
            },
            RecentDocuments = recentDocuments,
            RecentFolders = recentFolders
        };
    }
}
