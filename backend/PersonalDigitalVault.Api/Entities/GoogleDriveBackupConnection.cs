namespace PersonalDigitalVault.Api.Entities;

public class GoogleDriveBackupConnection
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    // Google OAuth refresh token is always encrypted at rest.
    // Access tokens are short-lived and are never persisted.
    public string RefreshTokenEncrypted { get; set; } = string.Empty;

    // Optional display-only account email, encrypted at rest.
    public string? AccountEmailEncrypted { get; set; }

    public bool AutoBackupEnabled { get; set; }

    // Allowed values: Daily, Weekly, Monthly.
    public string AutoBackupFrequency { get; set; } = "Weekly";

    public DateTime? LastBackupAt { get; set; }
    public DateTime? NextBackupAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
