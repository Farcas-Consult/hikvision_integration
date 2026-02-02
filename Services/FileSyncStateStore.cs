using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace hikvision_integration.Services;

public class FileSyncStateStore : ISyncStateStore
{
    private readonly string _filePath;
    private readonly ConcurrentDictionary<string, string> _state = new();
    private bool _loaded;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileSyncStateStore()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "hikvision-integration");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "sync-state.json");
    }

    public async Task<string?> GetLastFingerprintAsync(string memberId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _state.TryGetValue(memberId, out var fp) ? fp : null;
    }

    public Task SetLastFingerprintAsync(string memberId, string fingerprint, CancellationToken cancellationToken = default)
    {
        _state[memberId] = fingerprint;
        return Task.CompletedTask;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(_state.ToDictionary(x => x.Key, x => x.Value), new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded) return;
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_loaded) return;
            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    foreach (var kv in dict)
                        _state[kv.Key] = kv.Value;
                }
            }
            _loaded = true;
        }
        finally
        {
            _lock.Release();
        }
    }
}
