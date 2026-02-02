using System.Text.Json.Serialization;

namespace hikvision_integration.Models;

/// <summary>
/// Request payload for Hikvision ISAPI AccessControl UserInfo SetUp
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
