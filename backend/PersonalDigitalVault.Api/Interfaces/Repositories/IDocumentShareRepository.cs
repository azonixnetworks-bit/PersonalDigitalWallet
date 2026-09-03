using PersonalDigitalVault.Api.Entities;

namespace PersonalDigitalVault.Api.Interfaces.Repositories;

public interface IDocumentShareRepository
{
    // =====================================================
    // EXISTING SHARE CHECK
    // =====================================================
    //
    // INPUT:
    // documentId
    // recipientUserId
    //
    // REASON:
    // Same document same user-ku duplicate share
    // create aaga koodathu.
    //
    // OUTPUT:
    // Existing share / null
    //
    Task<DocumentShare?> GetExistingAsync(
        int documentId,
        int recipientUserId);


    // =====================================================
    // TOKEN HASH BASED SHARE FIND
    // =====================================================
    //
    // Email invitation-la varra token-a
    // backend hash pannum.
    //
    // Andha hash base panni pending invitation
    // find pannuvom.
    //
    Task<DocumentShare?> GetByTokenHashAsync(
        string tokenHash);


    // =====================================================
    // SHARED WITH ME
    // =====================================================
    //
    // Logged-in recipient-ku
    // accepted shares ellam return pannum.
    //
    Task<List<DocumentShare>> GetSharedWithMeAsync(
        int recipientUserId);


    // =====================================================
    // DOCUMENT OWNER SHARE LIST
    // =====================================================
    //
    // Owner:
    // "indha document yaara yaara user-ku
    // share pannirukken?"
    //
    // endra list.
    //
    Task<List<DocumentShare>> GetByDocumentForOwnerAsync(
        int documentId,
        int ownerUserId);


    // =====================================================
    // ACCEPTED SHARE ACCESS CHECK
    // =====================================================
    //
    // Document owner illaadha user
    // accepted share recipient-a irukkaara?
    //
    // Download permission-ku use pannuvom.
    //
    Task<bool> HasAcceptedAccessAsync(
        int documentId,
        int recipientUserId);


    // =====================================================
    // SPECIFIC SHARE OWNER CHECK
    // =====================================================
    //
    // Revoke panna:
    //
    // shareId
    // documentId
    // ownerUserId
    //
    // moonum match aaganum.
    //
    Task<DocumentShare?> GetOwnedShareAsync(
        int shareId,
        int documentId,
        int ownerUserId);


    // =====================================================
    // ADD SHARE
    // =====================================================

    Task AddAsync(
        DocumentShare share);


    // =====================================================
    // UPDATE SHARE
    // =====================================================
    //
    // Pending -> Accepted
    // Pending/Accepted -> Revoked
    // Re-share etc.
    //
    Task UpdateAsync(
        DocumentShare share);
}