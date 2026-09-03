using PersonalDigitalVault.Api.DTOs.Folder;
namespace PersonalDigitalVault.Api.Interfaces.Services;
public interface IFolderService { 
    Task<List<FolderDto>> GetAllAsync();
    Task<FolderDto> CreateAsync(CreateFolderDto dto);
    Task<FolderDto> UpdateAsync(int id, UpdateFolderDto dto); 
    Task DeleteAsync(int id);
}
