using PersonalDigitalVault.Api.DTOs.Share;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Helpers;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;

namespace PersonalDigitalVault.Api.Services;

public class DocumentShareService : IDocumentShareService
{
    // =========================================================
    // DEPENDENCIES
    // =========================================================

    private readonly IDocumentShareRepository _shareRepository;

    private readonly IDocumentRepository _documentRepository;

    private readonly IUserRepository _userRepository;

    private readonly CurrentUserService _currentUser;

    private readonly DocumentShareTokenService _tokenService;

    private readonly IEmailService _emailService;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Secure document sharing-ku thevaiyana
    // repositories + security + email services inject pannum.
    //
    // Input:
    // IDocumentShareRepository
    // IDocumentRepository
    // IUserRepository
    // CurrentUserService
    // DocumentShareTokenService
    // IEmailService
    //
    // Reason:
    // Share create / accept / list / revoke
    // ellame indha service handle pannum.
    //
    // Output:
    // Dependencies private fields-la store aagum.
    public DocumentShareService(
        IDocumentShareRepository shareRepository,
        IDocumentRepository documentRepository,
        IUserRepository userRepository,
        CurrentUserService currentUser,
        DocumentShareTokenService tokenService,
        IEmailService emailService)
    {
        _shareRepository =
            shareRepository;

        _documentRepository =
            documentRepository;

        _userRepository =
            userRepository;

        _currentUser =
            currentUser;

        _tokenService =
            tokenService;

        _emailService =
            emailService;
    }


    // =========================================================
    // CURRENT SECURE VAULT USER CHECK
    // =========================================================

    // Function:
    // Current authenticated user secure normal User-aa
    // irukkaar-aa verify pannum.
    //
    // Input:
    // JWT-lendhu Current UserId.
    //
    // Output:
    // Valid User entity.
    //
    // Security:
    //
    // ❌ Admin
    // ❌ Disabled account
    // ❌ Email verify incomplete
    // ❌ TOTP incomplete
    //
    // sharing module use panna mudiyadhu.
    private async Task<User> GetActiveCurrentUserAsync()
    {
        int userId =
            _currentUser.UserId;


        var user =
            await _userRepository
                .GetByIdAsync(
                    userId);


        // =====================================================
        // USER EXISTS + ACTIVE
        // =====================================================

        if (user == null ||
            !user.IsActive)
        {
            throw new UnauthorizedAccessException(
                "The current account is not available.");
        }


        // =====================================================
        // NORMAL USER ROLE ONLY
        // =====================================================

        // Admin private document sharing participant
        // aaga koodathu.
        if (!string.Equals(
                user.Role,
                "User",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                "Access denied.");
        }


        // =====================================================
        // SECURITY SETUP CHECK
        // =====================================================

        if (!user.IsEmailVerified ||
            !user.IsTotpEnabled ||
            string.IsNullOrWhiteSpace(
                user.TotpSecretEncrypted))
        {
            throw new UnauthorizedAccessException(
                "The current account security setup is incomplete.");
        }


        return user;
    }


    // =========================================================
    // INVALIDATE INVITATION TOKEN
    // =========================================================

    // Function:
    // Old invitation token DB hash-oda
    // match aagaama invalidate pannum.
    //
    // Input:
    // DocumentShare entity.
    //
    // Reason:
    //
    // Accepted token
    // Revoked token
    // Failed-email token
    //
    // thirumba use panna koodathu.
    //
    // Output:
    // InvitationTokenHash random new hash-a maarum.
    //
    // IMPORTANT:
    // Raw invalidation token DB-la save panna maatom.
    private void InvalidateInvitationToken(
        DocumentShare share)
    {
        var invalidationToken =
            _tokenService
                .GenerateToken();


        share.InvitationTokenHash =
            _tokenService
                .HashToken(
                    invalidationToken);
    }


    // =========================================================
    // SHARE DOCUMENT
    // =========================================================

    // API Service:
    // POST /api/documents/{documentId}/share
    //
    // Flow:
    //
    // Current secure User
    //          ↓
    // Own document?
    //          ↓
    // Recipient exists?
    //          ↓
    // Recipient = normal User?
    //          ↓
    // Active + Verified + TOTP?
    //          ↓
    // Duplicate accepted share?
    //          ↓
    // Secure random token
    //          ↓
    // Hash token DB-la
    //          ↓
    // Raw token email-la
    //
    // Output:
    // DocumentShareDto
    public async Task<DocumentShareDto> ShareDocumentAsync(
        int documentId,
        ShareDocumentRequestDto request)
    {
        // =====================================================
        // CURRENT OWNER
        // =====================================================

        var owner =
            await GetActiveCurrentUserAsync();


        int ownerUserId =
            owner.Id;


        // =====================================================
        // DOCUMENT OWNERSHIP CHECK
        // =====================================================

        // SECURITY:
        // Other user's DocumentId guess panninaalum
        // GetOwnedAsync current owner Id match aaganum.
        var document =
            await _documentRepository
                .GetOwnedAsync(
                    documentId,
                    ownerUserId);


        if (document == null)
        {
            throw new KeyNotFoundException(
                "Document not found.");
        }


        // =====================================================
        // REQUEST VALIDATION
        // =====================================================

        if (request == null ||
            string.IsNullOrWhiteSpace(
                request.RecipientEmail))
        {
            throw new ArgumentException(
                "Recipient email is required.");
        }


        string recipientEmail =
            request.RecipientEmail
                .Trim()
                .ToLowerInvariant();


        // =====================================================
        // FIND RECIPIENT
        // =====================================================

        var recipient =
            await _userRepository
                .GetByEmailAsync(
                    recipientEmail);


        if (recipient == null)
        {
            throw new ArgumentException(
                "A registered user with this email was not found.");
        }


        // =====================================================
        // SELF SHARE BLOCK
        // =====================================================

        if (recipient.Id ==
            ownerUserId)
        {
            throw new ArgumentException(
                "You cannot share a document with yourself.");
        }


        // =====================================================
        // RECIPIENT ROLE CHECK
        // =====================================================

        // IMPORTANT R5:
        //
        // Admin mattum block pannradhu pothaadhu.
        //
        // Exact normal User role mattum
        // document receive panna mudiyum.
        //
        // User    -> ✅
        // Admin   -> ❌
        // Unknown -> ❌
        if (!string.Equals(
                recipient.Role,
                "User",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The recipient account is not eligible for document sharing.");
        }


        // =====================================================
        // RECIPIENT ACTIVE CHECK
        // =====================================================

        if (!recipient.IsActive)
        {
            throw new ArgumentException(
                "The recipient account is disabled.");
        }


        // =====================================================
        // RECIPIENT EMAIL VERIFICATION
        // =====================================================

        if (!recipient.IsEmailVerified)
        {
            throw new ArgumentException(
                "The recipient has not completed email verification.");
        }


        // =====================================================
        // RECIPIENT TOTP SECURITY CHECK
        // =====================================================

        if (!recipient.IsTotpEnabled ||
            string.IsNullOrWhiteSpace(
                recipient.TotpSecretEncrypted))
        {
            throw new ArgumentException(
                "The recipient has not completed two-factor authentication setup.");
        }


        // =====================================================
        // EXISTING SHARE CHECK
        // =====================================================

        var existing =
            await _shareRepository
                .GetExistingAsync(
                    documentId,
                    recipient.Id);


        // Accepted share already irundha
        // duplicate invitation create panna vendaam.
        if (existing != null &&
            existing.Status ==
                DocumentShareStatus.Accepted)
        {
            throw new ArgumentException(
                "This document is already shared with this user.");
        }


        // =====================================================
        // GENERATE SECURE INVITATION TOKEN
        // =====================================================

        // Raw token email-ku mattum.
        var rawToken =
            _tokenService
                .GenerateToken();


        // Hash mattum database-la save pannuvom.
        var tokenHash =
            _tokenService
                .HashToken(
                    rawToken);


        DateTime now =
            DateTime.UtcNow;


        DocumentShare share;


        // =====================================================
        // NEW SHARE
        // =====================================================

        if (existing == null)
        {
            share =
                new DocumentShare
                {
                    DocumentId =
                        document.Id,

                    OwnerUserId =
                        ownerUserId,

                    RecipientUserId =
                        recipient.Id,

                    InvitationTokenHash =
                        tokenHash,

                    Status =
                        DocumentShareStatus.Pending,

                    CreatedAt =
                        now,

                    // Invitation 24 hours valid.
                    ExpiresAt =
                        now.AddHours(24),

                    AcceptedAt =
                        null,

                    RevokedAt =
                        null
                };


            await _shareRepository
                .AddAsync(
                    share);
        }

        // =====================================================
        // RE-SHARE / RESEND
        // =====================================================

        else
        {
            // Existing Pending / Revoked row reuse pannuvom.
            //
            // New token hash save pannumbodhu
            // previous email invitation automatically invalid.
            share =
                existing;


            share.OwnerUserId =
                ownerUserId;


            share.InvitationTokenHash =
                tokenHash;


            share.Status =
                DocumentShareStatus.Pending;


            share.CreatedAt =
                now;


            share.ExpiresAt =
                now.AddHours(24);


            share.AcceptedAt =
                null;


            share.RevokedAt =
                null;


            await _shareRepository
                .UpdateAsync(
                    share);
        }


        // =====================================================
        // SEND INVITATION EMAIL
        // =====================================================

        try
        {
            await _emailService
                .SendDocumentShareInvitationAsync(
                    recipient.Email,
                    recipient.FullName,
                    owner.FullName,
                    document.OriginalFileName,
                    rawToken);
        }
        catch
        {
            // =================================================
            // EMAIL FAILURE SECURITY
            // =================================================
            //
            // Email send fail aana share Pending-aa
            // database-la leave panna koodathu.
            //
            // Revoke + token invalidate pannuvom.
            share.Status =
                DocumentShareStatus.Revoked;


            share.RevokedAt =
                DateTime.UtcNow;


            InvalidateInvitationToken(
                share);


            await _shareRepository
                .UpdateAsync(
                    share);


            throw new InvalidOperationException(
                "The secure invitation email could not be sent.");
        }


        // =====================================================
        // RESPONSE DTO
        // =====================================================
        //
        // IMPORTANT:
        //
        // rawToken response-la return panna maatom.
        //
        // Token email-la mattum irukkum.
        return new DocumentShareDto
        {
            ShareId =
                share.Id,

            DocumentId =
                share.DocumentId,

            RecipientUserId =
                recipient.Id,

            RecipientName =
                recipient.FullName,

            RecipientEmail =
                recipient.Email,

            Status =
                share.Status,

            SharedAt =
                share.CreatedAt,

            ExpiresAt =
                share.ExpiresAt,

            AcceptedAt =
                share.AcceptedAt,

            RevokedAt =
                share.RevokedAt
        };
    }


    // =========================================================
    // ACCEPT SHARE INVITATION
    // =========================================================

    // API Service:
    // POST /api/shares/accept
    //
    // Flow:
    //
    // Raw token
    //    ↓
    // SHA-256 hash
    //    ↓
    // DB share
    //    ↓
    // Current secure User
    //    ↓
    // Correct recipient?
    //    ↓
    // Owner + recipient roles valid?
    //    ↓
    // Owner active?
    //    ↓
    // Document ownership consistent?
    //    ↓
    // Status Pending?
    //    ↓
    // Not expired?
    //    ↓
    // Accepted
    //    ↓
    // Token invalidated
    //
    // Output:
    // true  = accepted
    // false = invalid / expired / wrong user / revoked etc.
    public async Task<bool> AcceptShareAsync(
        AcceptShareRequestDto request)
    {
        // =====================================================
        // REQUEST TOKEN CHECK
        // =====================================================

        if (request == null ||
            string.IsNullOrWhiteSpace(
                request.Token))
        {
            return false;
        }


        // =====================================================
        // RAW TOKEN -> HASH
        // =====================================================

        var tokenHash =
            _tokenService
                .HashToken(
                    request.Token.Trim());


        // =====================================================
        // FIND SHARE
        // =====================================================

        var share =
            await _shareRepository
                .GetByTokenHashAsync(
                    tokenHash);


        // Invalid / old / already-used token.
        if (share == null)
        {
            return false;
        }


        // =====================================================
        // CURRENT SECURE USER
        // =====================================================

        // R5:
        // Old implementation current.UserId mattum
        // directly use pannuchu.
        //
        // Ippo active + User role + email + TOTP
        // full security check use pannrom.
        var currentUser =
            await GetActiveCurrentUserAsync();


        int currentUserId =
            currentUser.Id;


        // =====================================================
        // RECIPIENT ID MATCH
        // =====================================================

        // Email link vera user-ku forward panninaalum
        // selected recipient mattum accept panna mudiyum.
        if (share.RecipientUserId !=
            currentUserId)
        {
            return false;
        }


        // =====================================================
        // REQUIRED NAVIGATION DATA
        // =====================================================

        if (share.RecipientUser == null ||
            share.OwnerUser == null ||
            share.Document == null)
        {
            return false;
        }


        // =====================================================
        // RECIPIENT ENTITY CONSISTENCY
        // =====================================================

        // FK id and navigation user's Id
        // same-a irukkanum.
        if (share.RecipientUser.Id !=
            currentUserId)
        {
            return false;
        }


        // =====================================================
        // RECIPIENT ROLE CHECK
        // =====================================================

        if (!string.Equals(
                share.RecipientUser.Role,
                "User",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }


        // =====================================================
        // OWNER ROLE CHECK
        // =====================================================

        // Historical / tampered share record-la
        // owner Admin / unexpected role aana reject.
        if (!string.Equals(
                share.OwnerUser.Role,
                "User",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }


        // =====================================================
        // RECIPIENT ACTIVE CHECK
        // =====================================================

        if (!share.RecipientUser.IsActive)
        {
            return false;
        }


        // =====================================================
        // RECIPIENT SECURITY CHECK
        // =====================================================

        if (!share.RecipientUser.IsEmailVerified ||
            !share.RecipientUser.IsTotpEnabled ||
            string.IsNullOrWhiteSpace(
                share.RecipientUser.TotpSecretEncrypted))
        {
            return false;
        }


        // =====================================================
        // OWNER ACTIVE CHECK
        // =====================================================

        // Owner disabled-na pending invitation
        // accept panna mudiyadhu.
        if (!share.OwnerUser.IsActive)
        {
            return false;
        }


        // =====================================================
        // OWNER SECURITY CHECK
        // =====================================================

        if (!share.OwnerUser.IsEmailVerified ||
            !share.OwnerUser.IsTotpEnabled ||
            string.IsNullOrWhiteSpace(
                share.OwnerUser.TotpSecretEncrypted))
        {
            return false;
        }


        // =====================================================
        // DOCUMENT OWNER CONSISTENCY
        // =====================================================

        // Share OwnerUserId
        // and actual Document.UserId
        // same-a irukkanum.
        if (share.Document.UserId !=
            share.OwnerUserId)
        {
            return false;
        }


        // Navigation owner Id-um
        // share OwnerUserId-um match aaganum.
        if (share.OwnerUser.Id !=
            share.OwnerUserId)
        {
            return false;
        }


        // =====================================================
        // ONLY PENDING ACCEPTABLE
        // =====================================================

        // Accepted token replay:
        // ❌
        //
        // Revoked token:
        // ❌
        if (share.Status !=
            DocumentShareStatus.Pending)
        {
            return false;
        }


        // =====================================================
        // EXPIRY CHECK
        // =====================================================

        // IMPORTANT:
        // Accept status set panna munnaadi dhaan
        // expiry check nadakkanum.
        if (DateTime.UtcNow >=
            share.ExpiresAt)
        {
            return false;
        }


        // =====================================================
        // ACCEPT SHARE
        // =====================================================

        DateTime acceptedAt =
            DateTime.UtcNow;


        share.Status =
            DocumentShareStatus.Accepted;


        share.AcceptedAt =
            acceptedAt;


        share.RevokedAt =
            null;


        // =====================================================
        // SINGLE-USE TOKEN INVALIDATION
        // =====================================================

        // Email-la irundha original token
        // second request-la work aaga koodathu.
        InvalidateInvitationToken(
            share);


        // =====================================================
        // SAVE
        // =====================================================

        await _shareRepository
            .UpdateAsync(
                share);


        return true;
    }


    // =========================================================
    // SHARED WITH ME
    // =========================================================

    // API Service:
    // GET /api/shares/shared-with-me
    //
    // Output:
    // Current User-ku valid accepted shared documents.
    //
    // Security:
    //
    // ❌ Pending
    // ❌ Revoked
    // ❌ Different recipient
    // ❌ Disabled owner
    // ❌ Non-User owner
    // ❌ Document-owner mismatch
    //
    // results-la varakoodadhu.
    public async Task<List<SharedDocumentDto>>
        GetSharedWithMeAsync()
    {
        // =====================================================
        // CURRENT SECURE USER
        // =====================================================

        var currentUser =
            await GetActiveCurrentUserAsync();


        // =====================================================
        // GET REPOSITORY SHARES
        // =====================================================

        var shares =
            await _shareRepository
                .GetSharedWithMeAsync(
                    currentUser.Id);


        // =====================================================
        // DEFENSE-IN-DEPTH FILTER
        // =====================================================

        return shares
            .Where(
                share =>
                    // Required data.
                    share.Document != null
                    &&
                    share.OwnerUser != null

                    // Current recipient only.
                    &&
                    share.RecipientUserId ==
                        currentUser.Id

                    // Accepted share only.
                    &&
                    share.Status ==
                        DocumentShareStatus.Accepted

                    // Valid accepted state.
                    &&
                    share.AcceptedAt.HasValue

                    // Revoked share never show.
                    &&
                    share.RevokedAt == null

                    // Owner must still be active.
                    &&
                    share.OwnerUser.IsActive

                    // Owner must still be normal User.
                    &&
                    string.Equals(
                        share.OwnerUser.Role,
                        "User",
                        StringComparison.OrdinalIgnoreCase)

                    // Owner security setup must remain valid.
                    &&
                    share.OwnerUser.IsEmailVerified

                    &&
                    share.OwnerUser.IsTotpEnabled

                    &&
                    !string.IsNullOrWhiteSpace(
                        share.OwnerUser.TotpSecretEncrypted)

                    // Actual document owner and share owner
                    // must match.
                    &&
                    share.Document.UserId ==
                        share.OwnerUserId
            )
            .Select(
                share =>
                    new SharedDocumentDto
                    {
                        ShareId =
                            share.Id,

                        DocumentId =
                            share.DocumentId,

                        FileName =
                            share.Document!
                                .OriginalFileName,

                        ContentType =
                            share.Document
                                .ContentType,

                        FileSize =
                            share.Document
                                .FileSize,

                        SharedBy =
                            share.OwnerUser!
                                .FullName,

                        SharedByEmail =
                            share.OwnerUser
                                .Email,

                        Status =
                            share.Status,

                        SharedAt =
                            share.CreatedAt,

                        AcceptedAt =
                            share.AcceptedAt
                    })
            .ToList();
    }


    // =========================================================
    // OWNER DOCUMENT SHARE LIST
    // =========================================================

    // API Service:
    // GET /api/documents/{documentId}/shares
    //
    // Function:
    // Current document owner:
    //
    // Pending
    // Accepted
    // Revoked
    //
    // share history view pannalaam.
    //
    // Security:
    // Other user's document share records
    // view panna mudiyadhu.
    public async Task<List<DocumentShareDto>>
        GetDocumentSharesAsync(
            int documentId)
    {
        // =====================================================
        // CURRENT SECURE OWNER
        // =====================================================

        var owner =
            await GetActiveCurrentUserAsync();


        int ownerUserId =
            owner.Id;


        // =====================================================
        // DOCUMENT OWNERSHIP CHECK
        // =====================================================

        var document =
            await _documentRepository
                .GetOwnedAsync(
                    documentId,
                    ownerUserId);


        if (document == null)
        {
            throw new KeyNotFoundException(
                "Document not found.");
        }


        // =====================================================
        // GET OWNER SHARE RECORDS
        // =====================================================

        var shares =
            await _shareRepository
                .GetByDocumentForOwnerAsync(
                    documentId,
                    ownerUserId);


        // =====================================================
        // DTO MAPPING
        // =====================================================

        return shares
            .Where(
                share =>
                    share.RecipientUser != null

                    // Defense-in-depth:
                    // current owner relation must still match.
                    &&
                    share.OwnerUserId ==
                        ownerUserId

                    &&
                    share.DocumentId ==
                        documentId
            )
            .Select(
                share =>
                    new DocumentShareDto
                    {
                        ShareId =
                            share.Id,

                        DocumentId =
                            share.DocumentId,

                        RecipientUserId =
                            share.RecipientUserId,

                        RecipientName =
                            share.RecipientUser!
                                .FullName,

                        RecipientEmail =
                            share.RecipientUser
                                .Email,

                        Status =
                            share.Status,

                        SharedAt =
                            share.CreatedAt,

                        ExpiresAt =
                            share.ExpiresAt,

                        AcceptedAt =
                            share.AcceptedAt,

                        RevokedAt =
                            share.RevokedAt
                    })
            .ToList();
    }


    // =========================================================
    // REVOKE SHARE
    // =========================================================

    // API Service:
    // DELETE
    // /api/documents/{documentId}/shares/{shareId}
    //
    // Function:
    //
    // Owner:
    // Pending invitation revoke panna mudiyum.
    //
    // Owner:
    // Accepted recipient access revoke panna mudiyum.
    //
    // Security:
    // Other user share revoke panna mudiyadhu.
    //
    // Output:
    // true  = revoked / already revoked
    // false = not owner / not found
    public async Task<bool> RevokeShareAsync(
        int documentId,
        int shareId)
    {
        // =====================================================
        // CURRENT SECURE OWNER
        // =====================================================

        var owner =
            await GetActiveCurrentUserAsync();


        int ownerUserId =
            owner.Id;


        // =====================================================
        // OWNER + DOCUMENT + SHARE CHECK
        // =====================================================

        var share =
            await _shareRepository
                .GetOwnedShareAsync(
                    shareId,
                    documentId,
                    ownerUserId);


        if (share == null)
        {
            return false;
        }


        // =====================================================
        // DEFENSE-IN-DEPTH RELATION CHECK
        // =====================================================

        if (share.OwnerUserId !=
                ownerUserId ||
            share.DocumentId !=
                documentId ||
            share.Id !=
                shareId)
        {
            return false;
        }


        // =====================================================
        // ALREADY REVOKED
        // =====================================================

        // Repeat revoke idempotent success.
        if (share.Status ==
            DocumentShareStatus.Revoked)
        {
            return true;
        }


        // =====================================================
        // REVOKE
        // =====================================================

        share.Status =
            DocumentShareStatus.Revoked;


        share.RevokedAt =
            DateTime.UtcNow;


        // =====================================================
        // INVALIDATE OLD INVITATION TOKEN
        // =====================================================

        // Pending raw token / old email link
        // database token hash-oda match aaga koodathu.
        InvalidateInvitationToken(
            share);


        // =====================================================
        // SAVE
        // =====================================================

        await _shareRepository
            .UpdateAsync(
                share);


        return true;
    }
}