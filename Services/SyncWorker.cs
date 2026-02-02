using hikvision_integration.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace hikvision_integration.Services;

public class SyncWorker : BackgroundService
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncWorker> _logger;
    private readonly int _syncIntervalMinutes;

    public SyncWorker(
        ISyncService syncService,
        IOptions<SyncOptions> options,
        ILogger<SyncWorker> logger)
    {
        _syncService = syncService;
        _logger = logger;
        _syncIntervalMinutes = Math.Max(1, options.Value.SyncIntervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Sync worker started. Sync interval: {Interval} minutes",
            _syncIntervalMinutes);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_syncIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _syncService.RunSyncAsync(stoppingToken);

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Sync cycle completed: {Total} total, {Synced} synced, {Skipped} skipped, {Failed} failed",
                        result.TotalMembers, result.Synced, result.Skipped, result.Failed);
                }

                if (result.Errors.Count > 0 && _logger.IsEnabled(LogLevel.Warning))
                {
                    foreach (var err in result.Errors)
                        _logger.LogWarning("Sync error: {Error}", err);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync cycle failed");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }

        _logger.LogInformation("Sync worker stopped");
    }
}
