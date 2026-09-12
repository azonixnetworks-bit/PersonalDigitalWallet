using PersonalDigitalVault.Api.Interfaces.Services;

namespace PersonalDigitalVault.Api.Services;

public sealed class GoogleDriveAutoBackupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GoogleDriveAutoBackupWorker> _logger;

    public GoogleDriveAutoBackupWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<GoogleDriveAutoBackupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        do
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IGoogleDriveBackupService backupService =
                    scope.ServiceProvider.GetRequiredService<IGoogleDriveBackupService>();

                await backupService.ProcessDueAutomaticBackupsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Google Drive automatic backup cycle failed without exposing vault content.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
