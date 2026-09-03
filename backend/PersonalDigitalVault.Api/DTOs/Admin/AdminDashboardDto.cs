namespace PersonalDigitalVault.Api.DTOs.Admin;

public class AdminDashboardDto
{
    // Total registered users count
    public int TotalUsers { get; set; }

    // Current system-la total uploaded documents
    public int TotalUploads { get; set; }

    // Currently database-la irukkura stored files count
    public int TotalStoredFiles { get; set; }

    // Admin-ku file metadata மட்டும் காட்டும்
    // File content / path / encrypted data return panna maatom
    public List<AdminUploadDto> RecentUploads { get; set; }
        = new List<AdminUploadDto>();
}