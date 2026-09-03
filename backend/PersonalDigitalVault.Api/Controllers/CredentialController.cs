using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PersonalDigitalVault.Api.DTOs.Credential;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;

[ApiController]

// =========================================================
// AUTHORIZATION
// =========================================================
//
// Credential vault normal "User" role-ku mattum.
//
// Admin:
// ❌ credential create
// ❌ credential list
// ❌ password reveal
// ❌ credential update
// ❌ credential delete
//
// Security:
// Service-la EnsureVaultUser() already irukku.
// Controller-la idhu first HTTP authorization layer.
[Authorize(Roles = "User")]

[Route("api/credentials")]
public class CredentialController : ControllerBase
{
    // =========================================================
    // CREDENTIAL SERVICE
    // =========================================================

    private readonly ICredentialService _service;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Credential service controller-kulla inject pannum.
    //
    // Input:
    // ICredentialService
    //
    // Reason:
    // Controller direct repository / encryption access
    // panna koodathu.
    //
    // Output:
    // _service variable-la save aagum.
    public CredentialController(
        ICredentialService service)
    {
        _service =
            service;
    }


    // =========================================================
    // CREATE CREDENTIAL
    // =========================================================

    // API:
    // POST /api/credentials
    //
    // Input:
    // CreateCredentialDto
    //
    // Output:
    // Created CredentialDto.
    //
    // Security:
    // Owner UserId DTO-lendhu edukka maatom.
    // CredentialService current authenticated user Id use pannum.
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCredentialDto dto)
    {
        var result =
            await _service
                .CreateAsync(
                    dto);


        return Ok(
            result);
    }


    // =========================================================
    // GET MY CREDENTIALS
    // =========================================================

    // API:
    // GET /api/credentials
    //
    // Output:
    // Current user's credential list.
    //
    // Security:
    // Other users credentials return aaga koodathu.
    //
    // Password list response-la masked-a irukkum.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _service
                .GetAllAsync();


        return Ok(
            result);
    }


    // =========================================================
    // GET ONE CREDENTIAL
    // =========================================================

    // API:
    // GET /api/credentials/{id}
    //
    // Input:
    // Credential Id.
    //
    // Output:
    // Current owner credential details.
    //
    // Security:
    // CredentialService first ownership check pannitu
    // dhaan password decrypt pannum.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(
        int id)
    {
        var result =
            await _service
                .GetAsync(
                    id);


        return Ok(
            result);
    }


    // =========================================================
    // UPDATE CREDENTIAL
    // =========================================================

    // API:
    // PUT /api/credentials/{id}
    //
    // Input:
    // Credential Id
    // UpdateCredentialDto
    //
    // Output:
    // Updated CredentialDto.
    //
    // Security:
    // Current owner mattum update panna mudiyum.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateCredentialDto dto)
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
    // DELETE CREDENTIAL
    // =========================================================

    // API:
    // DELETE /api/credentials/{id}
    //
    // Input:
    // Credential Id.
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