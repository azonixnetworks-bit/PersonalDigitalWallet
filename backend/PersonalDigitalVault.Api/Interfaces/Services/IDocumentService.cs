using PersonalDigitalVault.Api.DTOs.Document;
namespace PersonalDigitalVault.Api.Interfaces.Services;
public interface IDocumentService 
{
    Task<DocumentDto> UploadAsync(UploadDocumentDto dto); 
    Task<List<DocumentDto>> GetAllAsync(); 
    Task<DocumentDto> GetAsync(int id); 
    Task<(byte[] Data,string ContentType,string FileName)> DownloadAsync(int id); 
    Task<DocumentDto> UpdateAsync(int id, UpdateDocumentDto dto); 
    Task DeleteAsync(int id); 
}
