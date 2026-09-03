using Microsoft.EntityFrameworkCore;

using PersonalDigitalVault.Api.Data;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Helpers;
using PersonalDigitalVault.Api.Interfaces.Repositories;

namespace PersonalDigitalVault.Api.Repositories;

public class DocumentShareRepository
    : IDocumentShareRepository
{
    // =========================================================
    // DATABASE CONTEXT
    // =========================================================

    private readonly AppDbContext _db;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // EF Core AppDbContext repository-kulla inject pannum.
    //
    // Input:
    // AppDbContext
    //
    // Reason:
    // DocumentShares table read/write operations handle panna.
    //
    // Output:
    // _db variable-la store aagum.
    public DocumentShareRepository(
        AppDbContext db)
    {
        _db =
            db;
    }


    // =========================================================
    // 1. GET EXISTING SHARE
    // =========================================================

    // Function:
    // Same document + same recipient-ku
    // already share row irukkaa check pannum.
    //
    // Input:
    // documentId
    // recipientUserId
    //
    // Output:
    // Existing DocumentShare
    // or null.
    //
    // Reason:
    // Duplicate DB row create pannaama
    // Pending / Revoked share-a reuse panna.
    public Task<DocumentShare?> GetExistingAsync(
        int documentId,
        int recipientUserId)
    {
        if (documentId <= 0 ||
            recipientUserId <= 0)
        {
            return Task.FromResult<DocumentShare?>(
                null);
        }


        return _db.DocumentShares
            .FirstOrDefaultAsync(
                share =>
                    share.DocumentId ==
                        documentId
                    &&
                    share.RecipientUserId ==
                        recipientUserId);
    }


    // =========================================================
    // 2. GET SHARE BY TOKEN HASH
    // =========================================================

    // Function:
    // Invitation raw token hash pannina value use panni
    // matching share record retrieve pannum.
    //
    // Input:
    // tokenHash
    //
    // Output:
    // Matching DocumentShare
    // or null.
    //
    // Security:
    // Raw token database-la save panna maatom.
    //
    // Navigation data:
    // Document
    // OwnerUser
    // RecipientUser
    //
    // accept-time security checks-ku required.
    public Task<DocumentShare?> GetByTokenHashAsync(
        string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(
                tokenHash))
        {
            return Task.FromResult<DocumentShare?>(
                null);
        }


        return _db.DocumentShares

            // Document still exists-aa
            // verify panna.
            .Include(
                share =>
                    share.Document)

            // Original owner active/security
            // verify panna.
            .Include(
                share =>
                    share.OwnerUser)

            // Selected recipient active/security
            // verify panna.
            .Include(
                share =>
                    share.RecipientUser)

            .FirstOrDefaultAsync(
                share =>
                    share.InvitationTokenHash ==
                        tokenHash);
    }


    // =========================================================
    // 3. SHARED WITH ME
    // =========================================================

    // Function:
    // Current recipient valid Accepted shares mattum
    // return pannum.
    //
    // Input:
    // recipientUserId
    //
    // Output:
    // List<DocumentShare>
    //
    // Security conditions:
    //
    // ✅ correct recipient
    // ✅ status Accepted
    // ✅ AcceptedAt exists
    // ✅ RevokedAt null
    // ✅ recipient exists
    // ✅ recipient active
    // ✅ recipient role exactly User
    // ✅ recipient email verified
    // ✅ recipient TOTP enabled
    // ✅ recipient TOTP secret exists
    // ✅ owner exists
    // ✅ owner active
    // ✅ owner role exactly User
    // ✅ owner email verified
    // ✅ owner TOTP enabled
    // ✅ owner TOTP secret exists
    // ✅ document exists
    // ✅ document owner == share owner
    public Task<List<DocumentShare>>
        GetSharedWithMeAsync(
            int recipientUserId)
    {
        if (recipientUserId <= 0)
        {
            return Task.FromResult(
                new List<DocumentShare>());
        }


        return _db.DocumentShares

            // Read-only query.
            .AsNoTracking()

            // File metadata.
            .Include(
                share =>
                    share.Document)

            // Owner metadata/security state.
            .Include(
                share =>
                    share.OwnerUser)

            // Recipient security state.
            .Include(
                share =>
                    share.RecipientUser)

            .Where(
                share =>

                    // =========================================
                    // CORRECT RECIPIENT
                    // =========================================

                    share.RecipientUserId ==
                        recipientUserId

                    &&

                    // =========================================
                    // ACCEPTED SHARE ONLY
                    // =========================================

                    share.Status ==
                        DocumentShareStatus.Accepted

                    &&

                    // Accepted share-na AcceptedAt
                    // mandatory.
                    share.AcceptedAt !=
                        null

                    &&

                    // Revoked share never return.
                    share.RevokedAt ==
                        null

                    &&

                    // =========================================
                    // RECIPIENT EXISTS
                    // =========================================

                    share.RecipientUser !=
                        null

                    &&

                    // =========================================
                    // RECIPIENT ACTIVE
                    // =========================================

                    share.RecipientUser.IsActive

                    &&

                    // =========================================
                    // RECIPIENT ROLE
                    // =========================================
                    //
                    // OLD:
                    //
                    // Role != "Admin"
                    //
                    // Problem:
                    // Unknown future role allow aagalaam.
                    //
                    // NEW:
                    //
                    // Exact User mattum.
                    share.RecipientUser.Role ==
                        "User"

                    &&

                    // =========================================
                    // RECIPIENT EMAIL VERIFIED
                    // =========================================

                    share.RecipientUser
                        .IsEmailVerified

                    &&

                    // =========================================
                    // RECIPIENT MFA ENABLED
                    // =========================================

                    share.RecipientUser
                        .IsTotpEnabled

                    &&

                    // TOTP enabled flag mattum illaama
                    // encrypted secret-um irukkanum.
                    share.RecipientUser
                        .TotpSecretEncrypted !=
                            null

                    &&

                    share.RecipientUser
                        .TotpSecretEncrypted !=
                            ""

                    &&

                    // =========================================
                    // OWNER EXISTS
                    // =========================================

                    share.OwnerUser !=
                        null

                    &&

                    // =========================================
                    // OWNER ACTIVE
                    // =========================================

                    share.OwnerUser.IsActive

                    &&

                    // =========================================
                    // OWNER ROLE
                    // =========================================
                    //
                    // Private vault owner exact User
                    // role-aa irukkanum.
                    share.OwnerUser.Role ==
                        "User"

                    &&

                    // =========================================
                    // OWNER EMAIL VERIFIED
                    // =========================================

                    share.OwnerUser
                        .IsEmailVerified

                    &&

                    // =========================================
                    // OWNER MFA READY
                    // =========================================

                    share.OwnerUser
                        .IsTotpEnabled

                    &&

                    share.OwnerUser
                        .TotpSecretEncrypted !=
                            null

                    &&

                    share.OwnerUser
                        .TotpSecretEncrypted !=
                            ""

                    &&

                    // =========================================
                    // DOCUMENT EXISTS
                    // =========================================

                    share.Document !=
                        null

                    &&

                    // =========================================
                    // DOCUMENT OWNER CONSISTENCY
                    // =========================================
                    //
                    // Share owner
                    //
                    // must equal
                    //
                    // actual Document.UserId.
                    //
                    // Tampered/inconsistent relationship-na
                    // share access block.
                    share.Document.UserId ==
                        share.OwnerUserId
            )

            // Latest share first.
            .OrderByDescending(
                share =>
                    share.CreatedAt)

            .ToListAsync();
    }


    // =========================================================
    // 4. OWNER DOCUMENT SHARE LIST
    // =========================================================

    // Function:
    // Original document owner:
    //
    // Pending
    // Accepted
    // Revoked
    //
    // share records view panna.
    //
    // Input:
    // documentId
    // ownerUserId
    //
    // Output:
    // Share list.
    //
    // Security:
    // Correct DocumentId + OwnerUserId
    // combination mattum.
    public Task<List<DocumentShare>>
        GetByDocumentForOwnerAsync(
            int documentId,
            int ownerUserId)
    {
        if (documentId <= 0 ||
            ownerUserId <= 0)
        {
            return Task.FromResult(
                new List<DocumentShare>());
        }


        return _db.DocumentShares

            // Read-only query.
            .AsNoTracking()

            // Frontend-ku recipient
            // name/email required.
            .Include(
                share =>
                    share.RecipientUser)

            .Where(
                share =>
                    share.DocumentId ==
                        documentId
                    &&
                    share.OwnerUserId ==
                        ownerUserId)

            .OrderByDescending(
                share =>
                    share.CreatedAt)

            .ToListAsync();
    }


    // =========================================================
    // 5. CHECK ACCEPTED DOWNLOAD ACCESS
    // =========================================================

    // Function:
    // Recipient-ku secure shared document download
    // permission irukkaa check pannum.
    //
    // Input:
    // documentId
    // recipientUserId
    //
    // Output:
    // true  -> allow shared download
    // false -> block
    //
    // IMPORTANT:
    // DocumentService DownloadAsync()-ku
    // idhu major security gate.
    //
    // Owner direct download separate owner check.
    public Task<bool> HasAcceptedAccessAsync(
        int documentId,
        int recipientUserId)
    {
        if (documentId <= 0 ||
            recipientUserId <= 0)
        {
            return Task.FromResult(
                false);
        }


        return _db.DocumentShares

            .AsNoTracking()

            .AnyAsync(
                share =>

                    // =========================================
                    // CORRECT DOCUMENT
                    // =========================================

                    share.DocumentId ==
                        documentId

                    &&

                    // =========================================
                    // CORRECT RECIPIENT
                    // =========================================

                    share.RecipientUserId ==
                        recipientUserId

                    &&

                    // =========================================
                    // ACCEPTED STATUS
                    // =========================================

                    share.Status ==
                        DocumentShareStatus.Accepted

                    &&

                    // =========================================
                    // ACCEPTED DATA CONSISTENCY
                    // =========================================

                    share.AcceptedAt !=
                        null

                    &&

                    // =========================================
                    // NOT REVOKED
                    // =========================================

                    share.RevokedAt ==
                        null

                    &&

                    // =========================================
                    // RECIPIENT EXISTS
                    // =========================================

                    share.RecipientUser !=
                        null

                    &&

                    // =========================================
                    // RECIPIENT ACTIVE
                    // =========================================

                    share.RecipientUser.IsActive

                    &&

                    // =========================================
                    // RECIPIENT ROLE
                    // =========================================
                    //
                    // Exact User role mattum.
                    //
                    // Admin / other privileged role
                    // private share download panna mudiyadhu.
                    share.RecipientUser.Role ==
                        "User"

                    &&

                    // =========================================
                    // RECIPIENT EMAIL VERIFIED
                    // =========================================

                    share.RecipientUser
                        .IsEmailVerified

                    &&

                    // =========================================
                    // RECIPIENT MFA ENABLED
                    // =========================================

                    share.RecipientUser
                        .IsTotpEnabled

                    &&

                    share.RecipientUser
                        .TotpSecretEncrypted !=
                            null

                    &&

                    share.RecipientUser
                        .TotpSecretEncrypted !=
                            ""

                    &&

                    // =========================================
                    // OWNER EXISTS
                    // =========================================

                    share.OwnerUser !=
                        null

                    &&

                    // =========================================
                    // OWNER ACTIVE
                    // =========================================

                    share.OwnerUser.IsActive

                    &&

                    // =========================================
                    // OWNER ROLE
                    // =========================================

                    share.OwnerUser.Role ==
                        "User"

                    &&

                    // =========================================
                    // OWNER EMAIL VERIFIED
                    // =========================================

                    share.OwnerUser
                        .IsEmailVerified

                    &&

                    // =========================================
                    // OWNER MFA ENABLED
                    // =========================================

                    share.OwnerUser
                        .IsTotpEnabled

                    &&

                    share.OwnerUser
                        .TotpSecretEncrypted !=
                            null

                    &&

                    share.OwnerUser
                        .TotpSecretEncrypted !=
                            ""

                    &&

                    // =========================================
                    // DOCUMENT EXISTS
                    // =========================================

                    share.Document !=
                        null

                    &&

                    // =========================================
                    // DOCUMENT OWNER CONSISTENCY
                    // =========================================

                    share.Document.UserId ==
                        share.OwnerUserId
            );
    }


    // =========================================================
    // 6. GET OWNER'S SPECIFIC SHARE
    // =========================================================

    // Function:
    // Revoke operation-ku exact share retrieve pannum.
    //
    // Input:
    // shareId
    // documentId
    // ownerUserId
    //
    // Output:
    // Matching DocumentShare
    // or null.
    //
    // Security:
    //
    // ShareId
    // +
    // DocumentId
    // +
    // OwnerUserId
    //
    // moonum match aaganum.
    //
    // So User A vera user's ShareId guess panninaalum
    // revoke panna mudiyadhu.
    public Task<DocumentShare?> GetOwnedShareAsync(
        int shareId,
        int documentId,
        int ownerUserId)
    {
        if (shareId <= 0 ||
            documentId <= 0 ||
            ownerUserId <= 0)
        {
            return Task.FromResult<DocumentShare?>(
                null);
        }


        return _db.DocumentShares
            .FirstOrDefaultAsync(
                share =>
                    share.Id ==
                        shareId
                    &&
                    share.DocumentId ==
                        documentId
                    &&
                    share.OwnerUserId ==
                        ownerUserId);
    }


    // =========================================================
    // 7. ADD SHARE
    // =========================================================

    // Function:
    // New DocumentShare row database-la save pannum.
    //
    // Input:
    // DocumentShare entity.
    //
    // Output:
    // SaveChanges complete.
    //
    // Used for:
    // First secure invitation.
    public async Task AddAsync(
        DocumentShare share)
    {
        if (share == null)
        {
            throw new ArgumentNullException(
                nameof(share));
        }


        _db.DocumentShares
            .Add(
                share);


        await _db
            .SaveChangesAsync();
    }


    // =========================================================
    // 8. UPDATE SHARE
    // =========================================================

    // Function:
    // Existing share state update pannum.
    //
    // Input:
    // DocumentShare entity.
    //
    // Used for:
    //
    // Pending -> Accepted
    // Accepted -> Revoked
    // Pending -> Resend
    // Email failed -> Revoked
    //
    // Output:
    // SaveChanges complete.
    public async Task UpdateAsync(
        DocumentShare share)
    {
        if (share == null)
        {
            throw new ArgumentNullException(
                nameof(share));
        }


        _db.DocumentShares
            .Update(
                share);


        await _db
            .SaveChangesAsync();
    }
}