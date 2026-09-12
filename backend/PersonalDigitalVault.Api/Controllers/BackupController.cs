using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalDigitalVault.Api.DTOs.Backup;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;
using System.Security.Cryptography;

namespace PersonalDigitalVault.Api.Controllers;

[ApiController]
[Route("api/backup")]
[Authorize]
public sealed class BackupController : ControllerBase
{
    private readonly IGoogleDriveBackupService _backupService;
    private readonly CurrentUserService _currentUser;
    private readonly IConfiguration _configuration;

    public BackupController(
        IGoogleDriveBackupService backupService,
        CurrentUserService currentUser,
        IConfiguration configuration)
    {
        _backupService = backupService;
        _currentUser = currentUser;
        _configuration = configuration;
    }

    [HttpGet("status")]
    public async Task<ActionResult<BackupStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        return Ok(await _backupService.GetStatusAsync(_currentUser.UserId, cancellationToken));
    }

    [HttpGet("google/connect-url")]
    public ActionResult<BackupConnectUrlDto> GetGoogleConnectUrl()
    {
        try
        {
            return Ok(new BackupConnectUrlDto
            {
                Url = _backupService.CreateAuthorizationUrl(_currentUser.UserId)
            });
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Google Drive backup is not configured",
                detail: exception.Message);
        }
    }

    [AllowAnonymous]
    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error) ||
            string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(state))
        {
            return Redirect(BuildFrontendRedirect("error"));
        }

        try
        {
            await _backupService.CompleteAuthorizationAsync(code, state, cancellationToken);
            return Redirect(BuildFrontendRedirect("connected"));
        }
        catch
        {
            // Never expose Google authorization codes, tokens, signed state,
            // encryption details or account information in the redirect.
            return Redirect(BuildFrontendRedirect("error"));
        }
    }

    [HttpPost("now")]
    public async Task<ActionResult<BackupHistoryItemDto>> BackupNow(CancellationToken cancellationToken)
    {
        try
        {
            BackupHistoryItemDto result = await _backupService.CreateBackupAsync(
                _currentUser.UserId,
                cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<BackupHistoryItemDto>>> GetHistory(
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _backupService.GetHistoryAsync(_currentUser.UserId, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("restore")]
    public async Task<ActionResult<RestoreBackupResultDto>> Restore(
        [FromBody] RestoreBackupRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _backupService.RestoreAsync(
                _currentUser.UserId,
                request,
                cancellationToken));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (CryptographicException)
        {
            return BadRequest(new { message = "Backup integrity verification failed." });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("automatic")]
    public async Task<ActionResult<BackupActionResultDto>> UpdateAutomaticBackup(
        [FromBody] AutomaticBackupRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _backupService.UpdateAutomaticBackupAsync(
                _currentUser.UserId,
                request,
                cancellationToken);

            return Ok(new BackupActionResultDto
            {
                Message = request.Enabled
                    ? "Automatic Google Drive backup enabled."
                    : "Automatic Google Drive backup disabled."
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("google")]
    public async Task<ActionResult<BackupActionResultDto>> DisconnectGoogleDrive(
        CancellationToken cancellationToken)
    {
        await _backupService.DisconnectAsync(_currentUser.UserId, cancellationToken);
        return Ok(new BackupActionResultDto
        {
            Message = "Google Drive disconnected. Existing backups remain in the Google account until removed by the account owner."
        });
    }

    private string BuildFrontendRedirect(string status)
    {
        string configured = _configuration["GoogleDrive:FrontendRedirectUri"]
            ?? "http://localhost:4200/backup";

        if (!Uri.TryCreate(configured, UriKind.Absolute, out Uri? baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            baseUri = new Uri("http://localhost:4200/backup");
        }

        var builder = new UriBuilder(baseUri);
        string separator = string.IsNullOrWhiteSpace(builder.Query) ? string.Empty : builder.Query.TrimStart('?') + "&";
        builder.Query = separator + "google=" + Uri.EscapeDataString(status);
        return builder.Uri.ToString();
    }
}
