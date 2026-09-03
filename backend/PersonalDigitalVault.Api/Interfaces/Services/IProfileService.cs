using PersonalDigitalVault.Api.DTOs.Profile;
namespace PersonalDigitalVault.Api.Interfaces.Services;
public interface IProfileService 
{ 
    
    Task<ProfileDto> GetAsync();
    Task<ProfileDto> UpdateAsync(UpdateProfileDto dto); 
}
