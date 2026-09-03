using PersonalDigitalVault.Api.Entities;
namespace PersonalDigitalVault.Api.Interfaces.Repositories;
public interface ICredentialRepository
{
    Task<List<Credential>> GetByUserAsync(int userId); 
    Task<Credential?> GetOwnedAsync(int id, int userId); 
    Task AddAsync(Credential credential);
    Task UpdateAsync(Credential credential); 
    Task DeleteAsync(Credential credential);
}
