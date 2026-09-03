using PersonalDigitalVault.Api.DTOs.Dashboard;

namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync();
}
