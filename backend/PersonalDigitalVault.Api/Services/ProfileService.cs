using PersonalDigitalVault.Api.DTOs.Profile; 
using PersonalDigitalVault.Api.Interfaces.Repositories; 
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;

namespace PersonalDigitalVault.Api.Services;
public class ProfileService(IUserRepository users,CurrentUserService current) : IProfileService
{
    public async Task<ProfileDto> GetAsync()
    {
        var u=await users.GetByIdAsync(current.UserId)??throw new KeyNotFoundException("User not found.");
        return new ProfileDto{Id=u.Id,FullName=u.FullName,Email=u.Email};
    }
    public async Task<ProfileDto> UpdateAsync(UpdateProfileDto dto)
    {
        var u=await users.GetByIdAsync(current.UserId)??throw new KeyNotFoundException("User not found.");
        u.FullName=dto.FullName.Trim();await users.UpdateAsync(u);return new ProfileDto{Id=u.Id,FullName=u.FullName,Email=u.Email};
    }
}
