using Microsoft.EntityFrameworkCore;
using PersonalDigitalVault.Api.Data;
using PersonalDigitalVault.Api.DTOs.Backup;
using PersonalDigitalVault.Api.Entities;
using PersonalDigitalVault.Api.Interfaces.Services;
using PersonalDigitalVault.Api.Security;
using System.Security.Cryptography;

namespace PersonalDigitalVault.Api.Services;

public sealed class GoogleDriveBackupService : IGoogleDriveBackupService
{
    private readonly AppDbContext _db;
    private readonly AesEncryption _aes;
    private readonly GoogleOAuthStateService _stateService;
    private readonly GoogleDriveClient _googleDrive;
    private readonly VaultBackupPackageService _packageService;
    private readonly TotpService _totpService;
    private readonly TotpSecretProtector _totpProtector;

    public GoogleDriveBackupService(
        AppDbContext db,
        AesEncryption aes,
        GoogleOAuthStateService stateService,
        GoogleDriveClient googleDrive,
        VaultBackupPackageService packageService,
        TotpService totpService,
        TotpSecretProtector totpProtector)
    {
        _db = db;
        _aes = aes;
        _stateService = stateService;
        _googleDrive = googleDrive;
        _packageService = packageService;
        _totpService = totpService;
        _totpProtector = totpProtector;
    }

    public async Task<BackupStatusDto> GetStatusAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        bool configured = _googleDrive.IsConfigured && _stateService.IsConfigured;
        GoogleDriveBackupConnection? connection = await _db.GoogleDriveBackupConnections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        string? email = null;
        if (connection is not null && !string.IsNullOrWhiteSpace(connection.AccountEmailEncrypted))
        {
            try
            {
                email = _aes.DecryptString(connection.AccountEmailEncrypted);
            }
            catch (CryptographicException)
            {
                email = null;
            }
        }

        return new BackupStatusDto
        {
            IsConfigured = configured,
            IsConnected = connection is not null,
            Provider = "GoogleDrive",
            AccountEmail = email,
            AutoBackupEnabled = connection?.AutoBackupEnabled ?? false,
            AutoBackupFrequency = connection?.AutoBackupFrequency ?? "Weekly",
            LastBackupAt = connection?.LastBackupAt,
            NextBackupAt = connection?.NextBackupAt
        };
    }

    public string CreateAuthorizationUrl(int userId)
    {
        EnsureFeatureConfigured();
        string state = _stateService.Create(userId, TimeSpan.FromMinutes(10));
        return _googleDrive.CreateAuthorizationUrl(state);
    }

    public async Task CompleteAuthorizationAsync(
        string code,
        string state,
        CancellationToken cancellationToken = default)
    {
        EnsureFeatureConfigured();
        int userId = _stateService.ValidateAndGetUserId(state);

        User? user = await _db.Users.SingleOrDefaultAsync(
            x => x.Id == userId && x.IsActive,
            cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("PDV account is not available.");
        }

        GoogleOAuthTokens tokens = await _googleDrive.ExchangeAuthorizationCodeAsync(
            code,
            cancellationToken);

        GoogleDriveBackupConnection? connection = await _db.GoogleDriveBackupConnections
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (connection is null)
        {
            connection = new GoogleDriveBackupConnection
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                AutoBackupFrequency = "Weekly"
            };
            _db.GoogleDriveBackupConnections.Add(connection);
        }

        connection.RefreshTokenEncrypted = _aes.EncryptString(tokens.RefreshToken);
        connection.AccountEmailEncrypted = string.IsNullOrWhiteSpace(tokens.AccountEmail)
            ? null
            : _aes.EncryptString(tokens.AccountEmail.Trim());
        connection.UpdatedAt = DateTime.UtcNow;

        if (connection.AutoBackupEnabled && connection.NextBackupAt is null)
        {
            connection.NextBackupAt = CalculateNextBackup(
                DateTime.UtcNow,
                connection.AutoBackupFrequency);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<BackupHistoryItemDto> CreateBackupAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        EnsureFeatureConfigured();
        GoogleDriveBackupConnection connection = await GetConnectionAsync(userId, cancellationToken);
        string accessToken = await GetAccessTokenAsync(connection, cancellationToken);
        VaultBackupPackage package = await _packageService.CreateAsync(userId, cancellationToken);

        try
        {
            BackupHistoryItemDto uploaded = await _googleDrive.UploadBackupAsync(
                accessToken,
                package.LocalPath,
                package.FileName,
                package.IntegrityHash,
                cancellationToken);

            DateTime completedAt = DateTime.UtcNow;
            connection.LastBackupAt = completedAt;
            connection.UpdatedAt = completedAt;
            connection.NextBackupAt = connection.AutoBackupEnabled
                ? CalculateNextBackup(completedAt, connection.AutoBackupFrequency)
                : null;
            await _db.SaveChangesAsync(cancellationToken);

            return uploaded;
        }
        finally
        {
            VaultBackupPackageService.DeleteTemporaryPackage(package.LocalPath);
        }
    }

    public async Task<IReadOnlyList<BackupHistoryItemDto>> GetHistoryAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        EnsureFeatureConfigured();
        GoogleDriveBackupConnection connection = await GetConnectionAsync(userId, cancellationToken);
        string accessToken = await GetAccessTokenAsync(connection, cancellationToken);
        return await _googleDrive.ListBackupsAsync(accessToken, cancellationToken);
    }

    public async Task<RestoreBackupResultDto> RestoreAsync(
        int userId,
        RestoreBackupRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureFeatureConfigured();

        if (request is null || string.IsNullOrWhiteSpace(request.FileId))
        {
            throw new InvalidOperationException("Backup file is required.");
        }

        await VerifyRestoreStepUpAsync(userId, request.TotpCode, cancellationToken);

        GoogleDriveBackupConnection connection = await GetConnectionAsync(userId, cancellationToken);
        string accessToken = await GetAccessTokenAsync(connection, cancellationToken);
        GoogleDriveDownloadedBackup download = await _googleDrive.DownloadBackupAsync(
            accessToken,
            request.FileId.Trim(),
            cancellationToken);

        try
        {
            return await _packageService.RestoreAsync(
                userId,
                download.LocalPath,
                download.ExpectedIntegrityHash,
                cancellationToken);
        }
        finally
        {
            VaultBackupPackageService.DeleteTemporaryPackage(download.LocalPath);
        }
    }

    public async Task UpdateAutomaticBackupAsync(
        int userId,
        AutomaticBackupRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new InvalidOperationException("Automatic backup settings are required.");
        }

        GoogleDriveBackupConnection connection = await GetConnectionAsync(userId, cancellationToken);
        string frequency = NormalizeFrequency(request.Frequency);

        connection.AutoBackupEnabled = request.Enabled;
        connection.AutoBackupFrequency = frequency;
        connection.NextBackupAt = request.Enabled
            ? CalculateNextBackup(DateTime.UtcNow, frequency)
            : null;
        connection.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DisconnectAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        GoogleDriveBackupConnection? connection = await _db.GoogleDriveBackupConnections
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (connection is null)
        {
            return;
        }

        string? refreshToken = null;
        try
        {
            refreshToken = _aes.DecryptString(connection.RefreshTokenEncrypted);
        }
        catch (CryptographicException)
        {
            // Local removal still proceeds even if the stored token is corrupted.
        }

        _db.GoogleDriveBackupConnections.Remove(connection);
        await _db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await _googleDrive.RevokeAsync(refreshToken, cancellationToken);
        }
    }

    public async Task ProcessDueAutomaticBackupsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_googleDrive.IsConfigured || !_stateService.IsConfigured)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        List<int> dueUserIds = await _db.GoogleDriveBackupConnections
            .AsNoTracking()
            .Where(x => x.AutoBackupEnabled &&
                        x.NextBackupAt.HasValue &&
                        x.NextBackupAt <= now)
            .OrderBy(x => x.NextBackupAt)
            .Select(x => x.UserId)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (int userId in dueUserIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await CreateBackupAsync(userId, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                GoogleDriveBackupConnection? connection = await _db.GoogleDriveBackupConnections
                    .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

                if (connection is not null)
                {
                    // Retry transient/authorization failures later without tight loops.
                    connection.NextBackupAt = DateTime.UtcNow.AddHours(6);
                    connection.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }

    private async Task VerifyRestoreStepUpAsync(
        int userId,
        string totpCode,
        CancellationToken cancellationToken)
    {
        User? user = await _db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null ||
            !user.IsActive ||
            !user.IsTotpEnabled ||
            string.IsNullOrWhiteSpace(user.TotpSecretEncrypted))
        {
            throw new UnauthorizedAccessException("TOTP step-up authentication is required to restore a backup.");
        }

        string secret;
        try
        {
            secret = _totpProtector.Decrypt(user.TotpSecretEncrypted);
        }
        catch
        {
            throw new UnauthorizedAccessException("TOTP step-up authentication failed.");
        }

        if (!_totpService.VerifyCode(secret, totpCode, out long matchedTimeStep))
        {
            throw new UnauthorizedAccessException("Invalid authenticator code.");
        }

        if (user.LastTotpTimeStepUsed.HasValue &&
            matchedTimeStep <= user.LastTotpTimeStepUsed.Value)
        {
            throw new UnauthorizedAccessException("Authenticator code has already been used.");
        }

        // Consume the TOTP before any destructive/restore work starts.
        user.LastTotpTimeStepUsed = matchedTimeStep;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<GoogleDriveBackupConnection> GetConnectionAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        return await _db.GoogleDriveBackupConnections
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Google Drive is not connected.");
    }

    private async Task<string> GetAccessTokenAsync(
        GoogleDriveBackupConnection connection,
        CancellationToken cancellationToken)
    {
        string refreshToken;
        try
        {
            refreshToken = _aes.DecryptString(connection.RefreshTokenEncrypted);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException("Google Drive connection data is corrupted. Please reconnect Google Drive.");
        }

        return await _googleDrive.RefreshAccessTokenAsync(refreshToken, cancellationToken);
    }

    private void EnsureFeatureConfigured()
    {
        if (!_googleDrive.IsConfigured || !_stateService.IsConfigured)
        {
            throw new InvalidOperationException("Google Drive backup is not configured on this PDV server.");
        }
    }

    private static string NormalizeFrequency(string? value)
    {
        if (string.Equals(value, "Daily", StringComparison.OrdinalIgnoreCase))
        {
            return "Daily";
        }

        if (string.Equals(value, "Weekly", StringComparison.OrdinalIgnoreCase))
        {
            return "Weekly";
        }

        if (string.Equals(value, "Monthly", StringComparison.OrdinalIgnoreCase))
        {
            return "Monthly";
        }

        throw new InvalidOperationException("Backup frequency must be Daily, Weekly, or Monthly.");
    }

    private static DateTime CalculateNextBackup(DateTime fromUtc, string frequency)
    {
        return NormalizeFrequency(frequency) switch
        {
            "Daily" => fromUtc.AddDays(1),
            "Weekly" => fromUtc.AddDays(7),
            "Monthly" => fromUtc.AddMonths(1),
            _ => fromUtc.AddDays(7)
        };
    }
}
