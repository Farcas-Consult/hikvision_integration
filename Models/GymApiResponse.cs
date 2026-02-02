using System.Text.Json.Serialization;

namespace hikvision_integration.Models;

public class GymApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public List<GymMember> Data { get; set; } = [];
}
