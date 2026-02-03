namespace hikvision_integration.Configuration;

public class SyncOptions
{
    public const string SectionName = "Sync";

    /// <summary>Interval between sync cycles in minutes (default: 1).</summary>
    public int SyncIntervalMinutes { get; set; } = 1;

    public GymOptions Gym { get; set; } = new();
    public HikvisionOptions Hikvision { get; set; } = new();
}

public class GymOptions
{
    public string ApiUrl { get; set; } = string.Empty;

    public string? ApiKey { get; set; }
}

public class HikvisionOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public List<string> ReaderUrls { get; set; } = [];
}
