using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;

[ApiController]

// =========================================================
// AUTHORIZATION
// =========================================================
//
// Private vault search normal User role-ku mattum.
//
// Admin:
// ❌ user folders search
// ❌ user documents search
// ❌ user credentials search
//
// Security:
// SearchService-layum EnsureVaultUser() irukku.
// Idhu HTTP boundary first protection layer.
[Authorize(Roles = "User")]

[Route("api/search")]
public class SearchController : ControllerBase
{
    // =========================================================
    // SEARCH SERVICE
    // =========================================================

    private readonly ISearchService _service;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Search service controller-kulla inject pannum.
    //
    // Input:
    // ISearchService
    //
    // Reason:
    // Controller direct repositories access panna koodathu.
    //
    // Output:
    // _service variable-la save aagum.
    public SearchController(
        ISearchService service)
    {
        _service =
            service;
    }


    // =========================================================
    // SEARCH MY VAULT
    // =========================================================

    // API:
    // GET /api/search?keyword=test
    //
    // Input:
    // keyword query parameter.
    //
    // Output:
    // Current user's matching:
    //
    // Folder
    // Document
    // Credential
    //
    // metadata results.
    //
    // Security:
    // SearchService current UserId filter use pannum.
    //
    // Other user records return aaga koodathu.
    //
    // Empty keyword-na SearchService empty list return pannum.
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string keyword = "")
    {
        var result =
            await _service
                .SearchAsync(
                    keyword);


        return Ok(
            result);
    }
}