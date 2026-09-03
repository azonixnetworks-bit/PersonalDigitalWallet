namespace PersonalDigitalVault.Api.DTOs.Admin;

public class AdminUploadDto
{
    // எந்த user file upload பண்ணினார்
    public string UploadedBy { get; set; } = string.Empty;

    // Original file name மட்டும் Admin பார்க்க முடியும்
    public string FileName { get; set; } = string.Empty;

    // File size bytes format-la database-lendhu வரும்
    public long FileSize { get; set; }

    // File upload date/time
    public DateTime UploadedAt { get; set; }
}