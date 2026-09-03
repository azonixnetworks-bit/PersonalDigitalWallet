using PersonalDigitalVault.Api.DTOs.Admin;

namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface IAdminService
{
    // Dashboard count return pannum
    Task<AdminDashboardDto> GetDashboardAsync();

    // Users list return pannum
    Task<List<UserListDto>> GetUsersAsync();

    // User enable / disable pannum
    Task UpdateUserStatusAsync(
        int id,
        UpdateUserStatusDto dto);
}