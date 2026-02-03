using System.Text.Json.Serialization;

namespace hikvision_integration.Models;

/// <summary>
/// Request payload for Hikvision ISAPI AccessControl UserInfo SetUp
/// todo: document error messages for myself.
/// </summary>
public class HikvisionUserInfoRequest
{
    [JsonPropertyName("UserInfo")]
    public HikvisionUserInfo UserInfo { get; set; } = new();
}

public class HikvisionUserInfo
{
    [JsonPropertyName("employeeNo")]
    public string EmployeeNo { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("userType")]
    public string UserType { get; set; } = "normal";

    [JsonPropertyName("Valid")]
    public HikvisionValid Valid { get; set; } = new();
}

public class HikvisionValid
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }

    [JsonPropertyName("beginTime")]
    public string BeginTime { get; set; } = "2026-01-01T23:59:59";

    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = "2030-01-01T23:59:59";
}

/// <summary>
/// Request for Hikvision ISAPI AccessControl UserInfo Search
/// </summary>
public class HikvisionUserInfoSearchRequest
{
    [JsonPropertyName("UserInfoSearchCond")]
    public UserInfoSearchCond SearchCond { get; set; } = new();
}

public class UserInfoSearchCond
{
    [JsonPropertyName("searchID")]
    public string SearchId { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("searchResultPosition")]
    public int SearchResultPosition { get; set; }

    [JsonPropertyName("maxResults")]
    public int MaxResults { get; set; } = 5000;
}

/// <summary>
/// Response from Hikvision ISAPI AccessControl UserInfo Search
/// </summary>
public class HikvisionUserInfoSearchResponse
{
    [JsonPropertyName("UserInfoSearch")]
    public UserInfoSearchResult? SearchResult { get; set; }
}

public class UserInfoSearchResult
{
    [JsonPropertyName("totalMatchNum")]
    public int TotalMatchNum { get; set; }

    [JsonPropertyName("searchID")]
    public string? SearchId { get; set; }

    [JsonPropertyName("responseStatusStrg")]
    public string? ResponseStatusStrg { get; set; }

    [JsonPropertyName("UserInfo")]
    public List<HikvisionUserInfo>? UserInfo { get; set; }
}
