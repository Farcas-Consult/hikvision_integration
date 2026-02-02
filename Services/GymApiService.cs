using System.Net.Http.Headers;
using System.Text.Json;
using hikvision_integration.Configuration;
using hikvision_integration.Models;
using Microsoft.Extensions.Options;

namespace hikvision_integration.Services;

public class GymApiService : IGymApiService
{
    private readonly HttpClient _httpClient;
    private readonly GymOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GymApiService(HttpClient httpClient, IOptions<SyncOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.Gym;
    }

    public async Task<IReadOnlyList<GymMember>> FetchMembersAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _options.ApiUrl);

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Add("x-api-key", _options.ApiKey);
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gym API failed: {response.StatusCode} {response.ReasonPhrase}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<GymApiResponse>(json, JsonOptions);

        if (apiResponse is null || !apiResponse.Success || apiResponse.Data is null)
        {
            throw new InvalidOperationException(
                $"Gym API returned invalid format: {json[..Math.Min(500, json.Length)]}...");
        }

        return apiResponse.Data;
    }
}
