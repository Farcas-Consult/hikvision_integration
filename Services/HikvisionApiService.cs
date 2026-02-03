using System.Text.Json;
using hikvision_integration.Configuration;
using hikvision_integration.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators.Digest;

namespace hikvision_integration.Services;

public class HikvisionApiService : IHikvisionApiService
{
    private readonly HikvisionOptions _options;
    private readonly ILogger<HikvisionApiService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public HikvisionApiService(IOptions<SyncOptions> options, ILogger<HikvisionApiService> logger)
    {
        _options = options.Value.Hikvision;
        _logger = logger;
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
        var baseUrlNormalized = baseUrl.TrimEnd('/');
        var requestPath = "/ISAPI/AccessControl/UserInfo/SetUp?format=json";

        var clientOptions = new RestClientOptions(baseUrlNormalized)
        {
            Authenticator = new DigestAuthenticator(_options.Username, _options.Password),
            ThrowOnAnyError = false,
            Timeout = TimeSpan.FromSeconds(30)
        };

        using var client = new RestClient(clientOptions);
        var payload = new HikvisionUserInfoRequest { UserInfo = userInfo };
        var body = JsonSerializer.Serialize(payload, JsonOptions);

        _logger.LogDebug("Syncing user {EmployeeNo} ({Name}) to {Url}",
            userInfo.EmployeeNo, userInfo.Name, baseUrlNormalized);

        var request = new RestRequest(requestPath, Method.Put)
            .AddStringBody(body, DataFormat.Json)
            .AddHeader("Content-Type", "application/json");

        var response = await client.ExecuteAsync(request, cancellationToken);

        if (!response.IsSuccessful)
        {
            var errorMsg = $"Hikvision API failed for {baseUrlNormalized}: {response.StatusCode} - {response.ErrorMessage ?? response.Content}";
            _logger.LogError(errorMsg);
            throw new HttpRequestException(errorMsg);
        }

        _logger.LogDebug("Successfully synced user {EmployeeNo} to {Url}", userInfo.EmployeeNo, baseUrlNormalized);
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
