using hikvision_integration.Models;

namespace hikvision_integration.Services;

public interface IGymApiService
{
    Task<IReadOnlyList<GymMember>> FetchMembersAsync(CancellationToken cancellationToken = default);
}
