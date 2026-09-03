using PersonalDigitalVault.Api.DTOs.Folder; using PersonalDigitalVault.Api.Entities; using PersonalDigitalVault.Api.Interfaces.Repositories; using PersonalDigitalVault.Api.Interfaces.Services; using PersonalDigitalVault.Api.Security;
namespace PersonalDigitalVault.Api.Services;
public class FolderService(IFolderRepository repo,CurrentUserService current) : IFolderService
{
    private static FolderDto Map(Folder x)=>new(){Id=x.Id,Name=x.Name,CreatedAt=x.CreatedAt};
    public async Task<List<FolderDto>> GetAllAsync()=>(await repo.GetByUserAsync(current.UserId)).Select(Map).ToList();
    public async Task<FolderDto> CreateAsync(CreateFolderDto dto){var x=new Folder{Name=dto.Name.Trim(),UserId=current.UserId};await repo.AddAsync(x);return Map(x);}
    public async Task<FolderDto> UpdateAsync(int id,UpdateFolderDto dto){var x=await repo.GetOwnedAsync(id,current.UserId)??throw new KeyNotFoundException("Folder not found.");x.Name=dto.Name.Trim();await repo.UpdateAsync(x);return Map(x);}
    public async Task DeleteAsync(int id){var x=await repo.GetOwnedAsync(id,current.UserId)??throw new KeyNotFoundException("Folder not found.");await repo.DeleteAsync(x);}
}
