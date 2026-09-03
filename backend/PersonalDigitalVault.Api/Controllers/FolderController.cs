using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PersonalDigitalVault.Api.DTOs.Folder;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;

[ApiController]

// =========================================================
// AUTHORIZATION
// =========================================================
//
// Private folder vault normal User role-ku mattum.
//
// Normal User:
// ✅ own folders list
// ✅ create folder
// ✅ own folder update
// ✅ own folder delete
//
// Admin:
// ❌ folder list
// ❌ create private folder
// ❌ update folder
// ❌ delete folder
//
// Security:
// FolderService ownership check separate-a irukku.
//
// So:
// Controller Role Check
// +
// Service Ownership Check
//
[Authorize(Roles = "User")]

[Route("api/folders")]
public class FolderController : ControllerBase
{
    // =========================================================
    // FOLDER SERVICE
    // =========================================================

    private readonly IFolderService _service;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Folder service controller-kulla inject pannum.
    //
    // Input:
    // IFolderService
    //
    // Reason:
    // Controller direct repository/database access
    // panna koodathu.
    //
    // Output:
    // _service variable-la save aagum.
    public FolderController(
        IFolderService service)
    {
        _service =
            service;
    }


    // =========================================================
    // GET MY FOLDERS
    // =========================================================

    // API:
    // GET /api/folders
    //
    // Output:
    // Current authenticated user's own folders.
    //
    // Security:
    // FolderService current UserId use panni
    // repository filtering pannum.
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result =
            await _service
                .GetAllAsync();


        return Ok(
            result);
    }


    // =========================================================
    // CREATE FOLDER
    // =========================================================

    // API:
    // POST /api/folders
    //
    // Input:
    // CreateFolderDto
    //
    // Output:
    // Created FolderDto.
    //
    // Security:
    // Folder owner UserId request body-lendhu varathu.
    //
    // Service current authenticated user Id use pannum.
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateFolderDto dto)
    {
        var result =
            await _service
                .CreateAsync(
                    dto);


        return Ok(
            result);
    }


    // =========================================================
    // UPDATE FOLDER
    // =========================================================

    // API:
    // PUT /api/folders/{id}
    //
    // Input:
    // Folder Id
    // UpdateFolderDto
    //
    // Output:
    // Updated FolderDto.
    //
    // Security:
    // Service:
    //
    // FolderId
    // +
    // Current UserId
    //
    // match aana mattum update.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateFolderDto dto)
    {
        var result =
            await _service
                .UpdateAsync(
                    id,
                    dto);


        return Ok(
            result);
    }


    // =========================================================
    // DELETE FOLDER
    // =========================================================

    // API:
    // DELETE /api/folders/{id}
    //
    // Input:
    // Folder Id.
    //
    // Output:
    // 204 No Content.
    //
    // Security:
    // Current owner mattum delete panna mudiyum.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id)
    {
        await _service
            .DeleteAsync(
                id);


        return NoContent();
    }
}