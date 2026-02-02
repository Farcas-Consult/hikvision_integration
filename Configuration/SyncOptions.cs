namespace hikvision_integration.Configuration;

public class SyncOptions
{
    public const string SectionName = "Sync";

    public GymOptions Gym { get; set; } = new();
    public HikvisionOptions Hikvision { get; set; } = new();
}

public class GymOptions
{
    /// <summary>Base URL for gym members API (e.g. https://api.gym.com/members)</summary>
    public string ApiUrl { get; set; } = string.Empty;

    /// <summary>Optional API key for authentication (sent as x-api-key header)</summary>
    public string? ApiKey { get; set; }
}

public class HikvisionOptions
{
    /// <summary>Hikvision device base URL (e.g. http://192.168.1.64)</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Username for digest authentication</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Password for digest authentication</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Multiple reader URLs. If set, overrides BaseUrl and syncs to each.</summary>
    public List<string> ReaderUrls { get; set; } = [];
}
