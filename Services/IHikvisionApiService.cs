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
}
