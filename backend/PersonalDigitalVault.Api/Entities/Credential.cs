namespace PersonalDigitalVault.Api.Entities;

public class Credential
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string UsernameEncrypted { get; set; } = string.Empty;
    public string PasswordEncrypted { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? NotesEncrypted { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
