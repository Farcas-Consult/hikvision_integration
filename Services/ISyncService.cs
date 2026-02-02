namespace hikvision_integration.Services;

public interface ISyncService
{
    Task<SyncResult> RunSyncAsync(CancellationToken cancellationToken = default);
}

public record SyncResult(int TotalMembers, int Synced, int Skipped, int Failed, IReadOnlyList<string> Errors);
