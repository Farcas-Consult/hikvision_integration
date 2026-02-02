using System.Security.Cryptography;
using System.Text;
using hikvision_integration.Models;
using Microsoft.Extensions.Logging;

namespace hikvision_integration.Services;

public class SyncService : ISyncService
{
    private readonly IGymApiService _gymApi;
    private readonly IHikvisionApiService _hikvisionApi;
    private readonly ISyncStateStore _stateStore;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        IGymApiService gymApi,
        IHikvisionApiService hikvisionApi,
        ISyncStateStore stateStore,
        ILogger<SyncService> logger)
    {
        _gymApi = gymApi;
        _hikvisionApi = hikvisionApi;
        _stateStore = stateStore;
        _logger = logger;
    }

    public async Task<SyncResult> RunSyncAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var synced = 0;
        var skipped = 0;
        var failed = 0;

        _logger.LogInformation("Fetching gym members...");
        IReadOnlyList<GymMember> members;

        try
        {
            members = await _gymApi.FetchMembersAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch gym members");
            return new SyncResult(0, 0, 0, 0, [ex.Message]);
        }

        _logger.LogInformation("Found {Count} members in gym system", members.Count);

        foreach (var member in members)
        {
            if (string.IsNullOrWhiteSpace(member.MemberId))
            {
                _logger.LogWarning("Skipping member with empty MemberId: {Name}", member.FullName);
                skipped++;
                continue;
            }

            var fingerprint = ComputeFingerprint(member);
            var lastFingerprint = await _stateStore.GetLastFingerprintAsync(member.MemberId, cancellationToken);

            if (lastFingerprint == fingerprint)
            {
                skipped++;
                continue;
            }

            var userInfo = MapToHikvision(member);

            try
            {
                await _hikvisionApi.SyncUserToAllReadersAsync(userInfo, cancellationToken);
                await _stateStore.SetLastFingerprintAsync(member.MemberId, fingerprint, cancellationToken);
                synced++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync member {MemberId} ({Name})", member.MemberId, member.FullName);
                errors.Add($"{member.MemberId}: {ex.Message}");
                failed++;
            }
        }

        await _stateStore.SaveAsync(cancellationToken);

        _logger.LogInformation(
            "Sync complete: {Total} total, {Synced} synced, {Skipped} skipped, {Failed} failed",
            members.Count, synced, skipped, failed);

        return new SyncResult(members.Count, synced, skipped, failed, errors);
    }

    private static string ComputeFingerprint(GymMember m)
    {
        var valid = m.Valid;
        var enable = valid?.Enable ?? m.IsActive;
        var begin = valid?.BeginTime ?? "";
        var end = valid?.EndTime ?? "";
        var payload = $"{m.MemberId}|{m.FullName}|{enable}|{begin}|{end}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }

    private static HikvisionUserInfo MapToHikvision(GymMember m)
    {
        var valid = m.Valid;
        var enable = valid?.Enable ?? (m.IsActive && string.Equals(m.MembershipStatus, "active", StringComparison.OrdinalIgnoreCase));

        return new HikvisionUserInfo
        {
            EmployeeNo = m.MemberId,
            Name = m.FullName,
            UserType = "normal",
            Valid = new HikvisionValid
            {
                Enable = enable,
                BeginTime = valid?.BeginTime ?? "2026-01-01T23:59:59",
                EndTime = valid?.EndTime ?? "2030-01-01T23:59:59"
            }
        };
    }
}
