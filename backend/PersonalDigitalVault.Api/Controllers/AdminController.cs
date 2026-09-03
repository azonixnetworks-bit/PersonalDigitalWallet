using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PersonalDigitalVault.Api.DTOs.Admin;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;

[ApiController]
[Route("api/admin")]


// =========================================================
// ADMIN AUTHORIZATION
// =========================================================
//
// Indha controller Admin role-ku mattum.
//
// Normal User:
// ❌ dashboard
// ❌ user list
// ❌ enable / disable accounts
//
// Security:
// AdminService-layum EnsureAdmin() irukku.
//
// So:
// Controller Role Check
// +
// Service Role Check
//
// rendu security layers.
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    // =========================================================
    // ADMIN SERVICE
    // =========================================================

    private readonly IAdminService _adminService;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Admin service controller-kulla inject pannum.
    //
    // Input:
    // IAdminService
    //
    // Reason:
    // Controller direct repository/database access
    // panna koodathu.
    //
    // Output:
    // _adminService variable-la save aagum.
    public AdminController(
        IAdminService adminService)
    {
        _adminService =
            adminService;
    }


    // =========================================================
    // ADMIN DASHBOARD
    // =========================================================

    // API:
    // GET /api/admin/dashboard
    //
    // Output:
    // Safe system metadata:
    //
    // Total users
    // Total uploads
    // Total stored document records
    // Recent upload metadata
    //
    // Security:
    // Document content / credentials / storage paths
    // return panna koodathu.
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var result =
            await _adminService
                .GetDashboardAsync();


        return Ok(
            result);
    }


    // =========================================================
    // GET USERS
    // =========================================================

    // API:
    // GET /api/admin/users
    //
    // Output:
    // Basic account metadata:
    //
    // Id
    // FullName
    // Email
    // Role
    // IsActive
    //
    // Security:
    // PasswordHash
    // OTP hash
    // TOTP secret
    // encryption data
    //
    // return panna koodathu.
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var result =
            await _adminService
                .GetUsersAsync();


        return Ok(
            result);
    }


    // =========================================================
    // ENABLE / DISABLE USER
    // =========================================================

    // API:
    // PUT /api/admin/users/{id}/status
    //
    // Example:
    //
    // PUT /api/admin/users/15/status
    //
    // Body:
    //
    // {
    //     "isActive": false
    // }
    //
    // Security:
    // AdminService normal "User" account mattum
    // status change panna allow pannum.
    //
    // Admin account / unknown role account
    // status change panna mudiyathu.
    [HttpPut("users/{id:int}/status")]
    public async Task<IActionResult> UpdateUserStatus(
        int id,
        [FromBody] UpdateUserStatusDto dto)
    {
        try
        {
            await _adminService
                .UpdateUserStatusAsync(
                    id,
                    dto);


            return Ok(
                new
                {
                    message =
                        dto.IsActive
                            ? "User enabled successfully."
                            : "User disabled successfully."
                });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(
                new
                {
                    message =
                        "User not found."
                });
        }
        catch (InvalidOperationException)
        {
            // Service exception raw message client-ku
            // directly expose panna maatom.
            return BadRequest(
                new
                {
                    message =
                        "User account status cannot be changed."
                });
        }
    }
}