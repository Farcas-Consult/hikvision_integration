namespace hikvision_integration.Services;

/// <summary>
/// Persists sync state to avoid redundant updates. Tracks last-known state per member.
/// </summary>
public interface ISyncStateStore
{
    Task<string?> GetLastFingerprintAsync(string memberId, CancellationToken cancellationToken = default);
    Task SetLastFingerprintAsync(string memberId, string fingerprint, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
}
