using PersonalDigitalVault.Api.DTOs.Profile;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Repositories;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;

namespace PersonalDigitalVault.Api.Services;

public class ProfileService(
    IUserRepository users,
    CurrentUserService current) : IProfileService
{
    public async Task<ProfileDto> GetAsync()
    {
        var user = await GetCurrentUserAsync();
        return Map(user);
    }

    public async Task<ProfileDto> UpdateAsync(UpdateProfileDto dto)
    {
        var user = await GetCurrentUserAsync();

        // Only the display name is editable from the profile endpoint.
        // Email, role and security state remain server-controlled.
        user.FullName = dto.FullName.Trim();

        await users.UpdateAsync(user);

        return Map(user);
    }

    private async Task<User> GetCurrentUserAsync()
    {
        return await users.GetByIdAsync(current.UserId)
            ?? throw new KeyNotFoundException("User not found.");
    }

    private static ProfileDto Map(User user)
    {
        return new ProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            IsTotpEnabled = user.IsTotpEnabled,
            CreatedAt = user.CreatedAt
        };
    }
}
