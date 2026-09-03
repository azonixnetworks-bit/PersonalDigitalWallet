using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PersonalDigitalVault.Api.DTOs.Document;
using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Controllers;

[ApiController]

// =========================================================
// DOCUMENT VAULT AUTHORIZATION
// =========================================================
//
// Private document vault normal User role-ku mattum.
//
// Normal User:
// ✅ own document upload
// ✅ own document list
// ✅ own metadata view
// ✅ own document update
// ✅ own document delete
// ✅ own document download
//
// Accepted Share Recipient:
// ✅ accepted shared document download
//
// Admin:
// ❌ upload
// ❌ list private documents
// ❌ metadata view
// ❌ download / decrypt
// ❌ update
// ❌ delete
//
// IMPORTANT:
// Shared recipient-um Role = User.
// So User-only authorization shared download-a break pannaathu.
[Authorize(Roles = "User")]

[Route("api/documents")]
public class DocumentController : ControllerBase
{
    // =========================================================
    // DOCUMENT SERVICE
    // =========================================================

    private readonly IDocumentService _service;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    // Function:
    // Document service controller-kulla inject pannum.
    //
    // Input:
    // IDocumentService
    //
    // Reason:
    // Controller direct repository / encryption /
    // SecureStorage access panna koodathu.
    //
    // Output:
    // _service variable-la store aagum.
    public DocumentController(
        IDocumentService service)
    {
        _service =
            service;
    }


    // =========================================================
    // UPLOAD DOCUMENT
    // =========================================================

    // API:
    // POST /api/documents/upload
    //
    // Input:
    // multipart/form-data
    //
    // UploadDocumentDto:
    // File
    // optional FolderId
    //
    // Output:
    // Uploaded DocumentDto.
    //
    // Security:
    // Actual file limit FileValidator-la 10 MB.
    //
    // Multipart transport overhead-ku controller
    // 11 MB request limit use pannum.
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentDto dto)
    {
        var result =
            await _service
                .UploadAsync(
                    dto);


        return Ok(
            result);
    }


    // =========================================================
    // GET MY DOCUMENTS
    // =========================================================

    // API:
    // GET /api/documents
    //
    // Output:
    // Current owner user's own documents.
    //
    // Shared-with-me documents inga include panna maatom.
    // Adhuku separate secure sharing endpoint irukku.
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
    // GET OWN DOCUMENT METADATA
    // =========================================================

    // API:
    // GET /api/documents/{id}
    //
    // Security:
    // Owner metadata endpoint.
    //
    // Accepted share recipient-ku download permission
    // irundhaalum indha owner metadata endpoint
    // automatically open panna maatom.
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
    // DOWNLOAD DOCUMENT
    // =========================================================

    // API:
    // GET /api/documents/{id}/download
    //
    // Security flow:
    //
    // Current User
    //      ↓
    // DocumentService
    //      ↓
    // Owner?
    //      ├── YES → allow
    //      ↓ NO
    // Accepted active share recipient?
    //      ├── YES → allow
    //      ↓ NO
    // Generic not found / reject
    //
    // IMPORTANT:
    // Admin Role controller level-la already blocked.
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(
        int id)
    {
        var file =
            await _service
                .DownloadAsync(
                    id);


        // File overload with filename use pannradhunaala
        // download response filename set aagum.
        return File(
            file.Data,
            file.ContentType,
            file.FileName);
    }


    // =========================================================
    // UPDATE DOCUMENT
    // =========================================================

    // API:
    // PUT /api/documents/{id}
    //
    // Security:
    // Owner-only operation.
    //
    // Shared recipient document rename panna mudiyathu.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateDocumentDto dto)
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
    // DELETE DOCUMENT
    // =========================================================

    // API:
    // DELETE /api/documents/{id}
    //
    // Security:
    // Owner-only operation.
    //
    // Shared recipient delete panna mudiyathu.
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