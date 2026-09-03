using PersonalDigitalVault.Api.DTOs.Admin;

namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface IAdminSubscriptionService
{
    // =========================================================
    // GET BILLING SUMMARY
    // =========================================================

    Task<AdminSubscriptionSummaryDto>
        GetSummaryAsync();


    // =========================================================
    // GET SAFE BILLING METADATA
    // =========================================================

    Task<List<AdminSubscriptionDto>>
        GetAllAsync();
}