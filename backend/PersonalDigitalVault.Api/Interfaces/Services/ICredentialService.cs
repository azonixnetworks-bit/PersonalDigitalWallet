using PersonalDigitalVault.Api.DTOs.Credential;
namespace PersonalDigitalVault.Api.Interfaces.Services;
public interface ICredentialService 
{ 
    Task<CredentialDto> CreateAsync(CreateCredentialDto dto); 
    Task<List<CredentialDto>> GetAllAsync();
    Task<CredentialDto> GetAsync(int id); 
    Task<CredentialDto> UpdateAsync(int id, UpdateCredentialDto dto); 
    Task DeleteAsync(int id); 
}
