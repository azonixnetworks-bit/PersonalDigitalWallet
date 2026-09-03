namespace PersonalDigitalVault.Api.DTOs.Share;

public class SharedDocumentDto
{
    // Share record ID
    public int ShareId { get; set; }


    // Actual Document ID
    public int DocumentId { get; set; }


    // User-ku display panna file name
    public string FileName { get; set; }
        = string.Empty;


    // MIME type
    //
    // Example:
    // application/pdf
    public string ContentType { get; set; }
        = string.Empty;


    // File size bytes
    public long FileSize { get; set; }


    // Share panna owner name
    public string SharedBy { get; set; }
        = string.Empty;


    // Owner email
    public string SharedByEmail { get; set; }
        = string.Empty;


    // Pending / Accepted / Revoked
    public string Status { get; set; }
        = string.Empty;


    // Share create panna time
    public DateTime SharedAt { get; set; }


    // Recipient accept panna time
    public DateTime? AcceptedAt { get; set; }
}