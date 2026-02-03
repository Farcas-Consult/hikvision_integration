using hikvision_integration.Models;

namespace hikvision_integration.Services;

public interface IHikvisionApiService
{
    /// <summary>
    /// Creates or updates a user on the Hikvision access control device.
    /// </summary>
    Task SyncUserAsync(HikvisionUserInfo userInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs a user to all configured Hikvision readers.
    /// </summary>
    Task SyncUserToAllReadersAsync(HikvisionUserInfo userInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the set of employeeNo values currently on the reader(s).
    /// Used to detect manual deletions - if a gym member is not on the reader, we re-sync them.
    /// Returns empty set if fetch fails (safe default: will force re-sync of all).
    /// </summary>
    Task<HashSet<string>> FetchReaderUserIdsAsync(CancellationToken cancellationToken = default);
}
