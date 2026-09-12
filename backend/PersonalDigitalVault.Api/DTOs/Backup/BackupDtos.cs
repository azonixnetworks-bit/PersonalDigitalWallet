namespace PersonalDigitalVault.Api.DTOs.Backup;

public sealed class BackupStatusDto
{
    public bool IsConfigured { get; set; }
    public bool IsConnected { get; set; }
    public string Provider { get; set; } = "GoogleDrive";
    public string? AccountEmail { get; set; }
    public bool AutoBackupEnabled { get; set; }
    public string AutoBackupFrequency { get; set; } = "Weekly";
    public DateTime? LastBackupAt { get; set; }
    public DateTime? NextBackupAt { get; set; }
}

public sealed class BackupConnectUrlDto
{
    public string Url { get; set; } = string.Empty;
}

public sealed class BackupHistoryItemDto
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? IntegrityHash { get; set; }
    public string Status { get; set; } = "Available";
}

public sealed class AutomaticBackupRequestDto
{
    public bool Enabled { get; set; }
    public string Frequency { get; set; } = "Weekly";
}

public sealed class RestoreBackupRequestDto
{
    public string FileId { get; set; } = string.Empty;
    public string TotpCode { get; set; } = string.Empty;
}

public sealed class RestoreBackupResultDto
{
    public int FoldersRestored { get; set; }
    public int DocumentsRestored { get; set; }
    public int CredentialsRestored { get; set; }
    public int ItemsSkipped { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class BackupActionResultDto
{
    public string Message { get; set; } = string.Empty;
}
