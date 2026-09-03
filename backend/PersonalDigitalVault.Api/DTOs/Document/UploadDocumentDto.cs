using Microsoft.AspNetCore.Http;
namespace PersonalDigitalVault.Api.DTOs.Document;
public class UploadDocumentDto { public IFormFile File { get; set; } = default!; public int? FolderId { get; set; } }