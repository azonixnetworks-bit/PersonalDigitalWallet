namespace PersonalDigitalVault.Api.DTOs.Share;

public class DocumentShareDto
{
    // Share record ID
    public int ShareId { get; set; }


    // Document ID
    public int DocumentId { get; set; }


    // Recipient User ID
    public int RecipientUserId { get; set; }


    // Recipient name
    public string RecipientName { get; set; }
        = string.Empty;


    // Recipient email
    public string RecipientEmail { get; set; }
        = string.Empty;


    // Pending / Accepted / Revoked
    public string Status { get; set; }
        = string.Empty;


    // Share create time
    public DateTime SharedAt { get; set; }


    // Email invitation expiry
    public DateTime ExpiresAt { get; set; }


    // Accepted time
    public DateTime? AcceptedAt { get; set; }


    // Revoke time
    public DateTime? RevokedAt { get; set; }
}