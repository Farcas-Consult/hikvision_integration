using System.Text.Json;
using hikvision_integration.Configuration;
using hikvision_integration.Models;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators.Digest;

namespace hikvision_integration.Services;

public class HikvisionApiService : IHikvisionApiService
{
    private readonly HikvisionOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public HikvisionApiService(IOptions<SyncOptions> options)
    {
        _options = options.Value.Hikvision;
    }

    public async Task SyncUserAsync(HikvisionUserInfo userInfo, CancellationToken cancellationToken = default)
    {
        var baseUrl = GetBaseUrl();
        await SyncUserToReaderAsync(baseUrl, userInfo, cancellationToken);
    }

    public async Task SyncUserToAllReadersAsync(HikvisionUserInfo userInfo, CancellationToken cancellationToken = default)
    {
        var readerUrls = GetReaderUrls();

        foreach (var readerUrl in readerUrls)
        {
            await SyncUserToReaderAsync(readerUrl, userInfo, cancellationToken);
        }
    }

    private async Task SyncUserToReaderAsync(string baseUrl, HikvisionUserInfo userInfo, CancellationToken cancellationToken = default)
    {
        var url = $"{baseUrl.TrimEnd('/')}/ISAPI/AccessControl/UserInfo/SetUp?format=json";

        var options = new RestClientOptions(url)
        {
            Authenticator = new DigestAuthenticator(_options.Username, _options.Password)
        };

        var client = new RestClient(options);
        var payload = new HikvisionUserInfoRequest { UserInfo = userInfo };
        var body = JsonSerializer.Serialize(payload, JsonOptions);

        var request = new RestRequest()
            .AddStringBody(body, DataFormat.Json)
            .AddHeader("Content-Type", "application/json");

        var response = await client.PutAsync(request, cancellationToken);

        if (!response.IsSuccessful)
        {
            throw new HttpRequestException(
                $"Hikvision API failed for {baseUrl}: {response.StatusCode} - {response.Content}");
        }
    }

    private string GetBaseUrl()
    {
        if (_options.ReaderUrls.Count > 0)
        {
            return _options.ReaderUrls[0].TrimEnd('/');
        }
        return _options.BaseUrl.TrimEnd('/');
    }

    private IEnumerable<string> GetReaderUrls()
    {
        if (_options.ReaderUrls.Count > 0)
        {
            return _options.ReaderUrls.Select(u => u.TrimEnd('/'));
        }
        return [_options.BaseUrl.TrimEnd('/')];
    }
}
