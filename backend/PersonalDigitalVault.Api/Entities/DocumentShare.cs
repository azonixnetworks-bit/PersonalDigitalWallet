using PersonalDigitalVault.Api.Helpers;

namespace PersonalDigitalVault.Api.Entities;

public class DocumentShare
{
    // =====================================================
    // PRIMARY KEY
    // =====================================================

    // DocumentShares table primary key
    public int Id { get; set; }


    // =====================================================
    // DOCUMENT
    // =====================================================

    // Endha document share pannirukkom
    public int DocumentId { get; set; }


    // Document navigation property
    public Document? Document { get; set; }


    // =====================================================
    // OWNER
    // =====================================================

    // File original owner UserId
    public int OwnerUserId { get; set; }


    // Owner user navigation
    public User? OwnerUser { get; set; }


    // =====================================================
    // RECIPIENT
    // =====================================================

    // File receive panna user
    public int RecipientUserId { get; set; }


    // Recipient navigation
    public User? RecipientUser { get; set; }


    // =====================================================
    // INVITATION TOKEN
    // =====================================================
    //
    // IMPORTANT:
    //
    // Email-la send panna original token-a
    // database-la plain text-aa save panna maatom.
    //
    // Token hash mattum save pannuvom.
    //
    public string InvitationTokenHash { get; set; }
        = string.Empty;


    // =====================================================
    // STATUS
    // =====================================================
    //
    // Pending
    // Accepted
    // Revoked
    //
    public string Status { get; set; }
        = DocumentShareStatus.Pending;


    // =====================================================
    // EXPIRY
    // =====================================================
    //
    // Email invitation expiry.
    //
    // Example:
    // CreatedAt + 24 hours
    //
    public DateTime ExpiresAt { get; set; }


    // =====================================================
    // CREATED DATE
    // =====================================================

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;


    // =====================================================
    // ACCEPTED DATE
    // =====================================================
    //
    // Recipient invitation accept panna
    // indha date save aagum.
    //
    public DateTime? AcceptedAt { get; set; }


    // =====================================================
    // REVOKED DATE
    // =====================================================
    //
    // Owner access remove pannina
    // date/time.
    //
    public DateTime? RevokedAt { get; set; }
}