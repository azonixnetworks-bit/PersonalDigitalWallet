using PersonalDigitalVault.Api.DTOs.Backup;

namespace PersonalDigitalVault.Api.Interfaces.Services;

public interface IGoogleDriveBackupService
{
    Task<BackupStatusDto> GetStatusAsync(int userId, CancellationToken cancellationToken = default);
    string CreateAuthorizationUrl(int userId);
    Task CompleteAuthorizationAsync(string code, string state, CancellationToken cancellationToken = default);
    Task<BackupHistoryItemDto> CreateBackupAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BackupHistoryItemDto>> GetHistoryAsync(int userId, CancellationToken cancellationToken = default);
    Task<RestoreBackupResultDto> RestoreAsync(int userId, RestoreBackupRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateAutomaticBackupAsync(int userId, AutomaticBackupRequestDto request, CancellationToken cancellationToken = default);
    Task DisconnectAsync(int userId, CancellationToken cancellationToken = default);
    Task ProcessDueAutomaticBackupsAsync(CancellationToken cancellationToken = default);
}
