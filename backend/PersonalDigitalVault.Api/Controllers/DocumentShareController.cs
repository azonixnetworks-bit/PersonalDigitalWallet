using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PersonalDigitalVault.Api.DTOs.Share;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;


// =========================================================
// DOCUMENT SHARE CONTROLLER
// =========================================================
//
// Secure document sharing normal User role-ku mattum.
//
// Admin:
// ❌ document share create
// ❌ share invitation accept
// ❌ shared-with-me
// ❌ owner share list
// ❌ revoke share
//
// Normal User:
// ✅ own document share
// ✅ invitation accept
// ✅ accepted shared documents view
// ✅ own document share list
// ✅ own share revoke
//
[ApiController]
[Authorize(Roles = "User")]
[Route("api")]
public class DocumentShareController : ControllerBase
{
    // =========================================================
    // DOCUMENT SHARE SERVICE
    // =========================================================

    private readonly IDocumentShareService _documentShareService;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Document sharing service controller-kulla inject pannum.
    //
    // Input:
    // IDocumentShareService
    //
    // Reason:
    // Controller direct repository/token/email logic
    // handle panna koodathu.
    //
    // Output:
    // _documentShareService-la store aagum.
    public DocumentShareController(
        IDocumentShareService documentShareService)
    {
        _documentShareService =
            documentShareService;
    }


    // =========================================================
    // 1. SHARE DOCUMENT
    // =========================================================

    // API:
    // POST /api/documents/{documentId}/share
    //
    // Body:
    //
    // {
    //     "recipientEmail": "user@example.com"
    // }
    //
    // Flow:
    //
    // Logged User
    //      ↓
    // Own Document?
    //      ↓
    // Recipient eligible?
    //      ↓
    // Secure random invite token
    //      ↓
    // Token HASH database-la
    //      ↓
    // RAW token email-la
    //
    // Security:
    // Exact recipient account state client-ku reveal panna maatom.
    [HttpPost(
        "documents/{documentId:int}/share"
    )]
    public async Task<IActionResult> ShareDocument(
        int documentId,
        [FromBody] ShareDocumentRequestDto request)
    {
        try
        {
            var result =
                await _documentShareService
                    .ShareDocumentAsync(
                        documentId,
                        request);


            return Ok(
                result);
        }
        catch (KeyNotFoundException)
        {
            // Document doesn't exist
            //
            // OR
            //
            // current user doesn't own it.
            //
            // Same generic result.
            return NotFound(
                new
                {
                    message =
                        "Document not found."
                });
        }
        catch (ArgumentException)
        {
            // Possible internal reasons:
            //
            // self share
            // recipient not registered
            // recipient Admin
            // recipient disabled
            // recipient email unverified
            // recipient TOTP incomplete
            // duplicate accepted share
            //
            // Exact reason expose panna maatom.
            return BadRequest(
                new
                {
                    message =
                        "The document could not be shared with this recipient."
                });
        }
        catch (UnauthorizedAccessException)
        {
            // Request authenticated-a irundhaalum
            // operation permission fail aana generic 403.
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message =
                        "Access denied."
                });
        }
        catch (InvalidOperationException)
        {
            // Example:
            // Secure invitation create/send process fail.
            //
            // SMTP/internal details expose panna maatom.
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message =
                        "The secure share invitation could not be created."
                });
        }
    }


    // =========================================================
    // 2. ACCEPT SHARE INVITATION
    // =========================================================

    // API:
    // POST /api/shares/accept
    //
    // Body:
    //
    // {
    //     "token": "raw-email-invitation-token"
    // }
    //
    // Security:
    //
    // Email link alone access kudukkaathu.
    //
    // User:
    // Login
    //   ↓
    // Password
    //   ↓
    // TOTP
    //   ↓
    // Final User JWT
    //   ↓
    // Invitation token
    //   ↓
    // Recipient match
    //   ↓
    // Accept
    [HttpPost(
        "shares/accept"
    )]
    public async Task<IActionResult> AcceptShare(
        [FromBody] AcceptShareRequestDto request)
    {
        var accepted =
            await _documentShareService
                .AcceptShareAsync(
                    request);


        if (!accepted)
        {
            // Do NOT reveal:
            //
            // wrong recipient
            // expired
            // revoked
            // already used
            // invalid token
            //
            // Same generic response.
            return BadRequest(
                new
                {
                    message =
                        "This secure share invitation cannot be accepted."
                });
        }


        return Ok(
            new
            {
                message =
                    "Document share accepted successfully."
            });
    }


    // =========================================================
    // 3. SHARED WITH ME
    // =========================================================

    // API:
    // GET /api/shares/shared-with-me
    //
    // Output:
    // Current recipient successfully accepted
    // shared documents mattum.
    //
    // Security:
    // Pending / revoked share return aaga koodathu.
    [HttpGet(
        "shares/shared-with-me"
    )]
    public async Task<IActionResult> GetSharedWithMe()
    {
        var result =
            await _documentShareService
                .GetSharedWithMeAsync();


        return Ok(
            result);
    }


    // =========================================================
    // 4. OWNER DOCUMENT SHARE LIST
    // =========================================================

    // API:
    // GET /api/documents/{documentId}/shares
    //
    // Purpose:
    // Owner indha document yaarukku share pannirukkaar
    // nu view panna.
    //
    // Security:
    // Current owner mattum list access panna mudiyum.
    [HttpGet(
        "documents/{documentId:int}/shares"
    )]
    public async Task<IActionResult> GetDocumentShares(
        int documentId)
    {
        try
        {
            var result =
                await _documentShareService
                    .GetDocumentSharesAsync(
                        documentId);


            return Ok(
                result);
        }
        catch (KeyNotFoundException)
        {
            // Other user's document existence
            // disclose panna maatom.
            return NotFound(
                new
                {
                    message =
                        "Document not found."
                });
        }
    }


    // =========================================================
    // 5. REVOKE DOCUMENT SHARE
    // =========================================================

    // API:
    // DELETE
    // /api/documents/{documentId}/shares/{shareId}
    //
    // Function:
    // Original document owner recipient access revoke pannum.
    //
    // Security:
    // Recipient itself revoke-owner operation panna mudiyathu.
    // Other user share revoke panna mudiyathu.
    [HttpDelete(
        "documents/{documentId:int}/shares/{shareId:int}"
    )]
    public async Task<IActionResult> RevokeShare(
        int documentId,
        int shareId)
    {
        bool revoked =
            await _documentShareService
                .RevokeShareAsync(
                    documentId,
                    shareId);


        if (!revoked)
        {
            return NotFound(
                new
                {
                    message =
                        "Document share was not found or access is not allowed."
                });
        }


        return Ok(
            new
            {
                message =
                    "Document access revoked successfully."
            });
    }
}