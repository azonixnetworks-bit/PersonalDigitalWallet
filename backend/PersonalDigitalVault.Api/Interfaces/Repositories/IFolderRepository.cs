using PersonalDigitalVault.Api.Entities;
namespace PersonalDigitalVault.Api.Interfaces.Repositories;
public interface IFolderRepository
{
    Task<List<Folder>> GetByUserAsync(int userId); 
    Task<Folder?> GetOwnedAsync(int id, int userId);
    Task AddAsync(Folder folder); 
    Task UpdateAsync(Folder folder); 
    Task DeleteAsync(Folder folder);
}
