using System.Text.Json.Serialization;

namespace hikvision_integration.Models;

public class GymMember
{
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("profilePictureUrl")]
    public string? ProfilePictureUrl { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("membershipStatus")]
    public string MembershipStatus { get; set; } = string.Empty;

    [JsonPropertyName("turnstileId")]
    public string? TurnstileId { get; set; }

    [JsonPropertyName("memberId")]
    public string MemberId { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("Valid")]
    public MemberValid? Valid { get; set; }

    [JsonPropertyName("lastUpdated")]
    public DateTimeOffset? LastUpdated { get; set; }
}

public class MemberValid
{
    [JsonPropertyName("enable")]
    public bool Enable { get; set; }

    [JsonPropertyName("beginTime")]
    public string? BeginTime { get; set; }

    [JsonPropertyName("endTime")]
    public string? EndTime { get; set; }
}
