namespace PersonalDigitalVault.Api.Entities;

public class Document
{
    // Database primary key
    public int Id { get; set; }


    // User upload panna original file name
    //
    // Example:
    // passport.pdf
    public string OriginalFileName { get; set; }
        = string.Empty;


    // SecureStorage-la use panna random filename
    //
    // Example:
    // abc123.vault
    public string StoredFileName { get; set; }
        = string.Empty;


    // MIME type
    //
    // Example:
    // application/pdf
    public string ContentType { get; set; }
        = string.Empty;


    // File size bytes-la
    public long FileSize { get; set; }


    // SHA-256 integrity hash
    public string FileHash { get; set; }
        = string.Empty;


    // Encrypted .vault file
    // server-la enga irukku endra path
    public string StoragePath { get; set; }
        = string.Empty;


    // Original owner
    public int UserId { get; set; }


    public User? User { get; set; }


    // Optional folder
    public int? FolderId { get; set; }


    public Folder? Folder { get; set; }


    // Upload date
    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;


    // Rename/update date
    public DateTime? UpdatedAt { get; set; }


    // =====================================================
    // DOCUMENT SHARING
    // =====================================================
    //
    // Indha document-ku associated
    // share permissions.
    //
    public ICollection<DocumentShare> Shares
    {
        get;
        set;
    }
        = new List<DocumentShare>();
}