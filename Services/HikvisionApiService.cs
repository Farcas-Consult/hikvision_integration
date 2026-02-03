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

    public async Task<HashSet<string>> FetchReaderUserIdsAsync(CancellationToken cancellationToken = default)
    {
        var readerUrls = GetReaderUrls().ToList();

        if (readerUrls.Count == 0)
            return [];

        HashSet<string>? intersection = null;

        foreach (var baseUrl in readerUrls)
        {
            try
            {
                var ids = await FetchUserIdsFromReaderAsync(baseUrl, cancellationToken);
                intersection = intersection is null
                    ? ids
                    : new HashSet<string>(intersection.Intersect(ids), StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch users from reader {Url}, will force re-sync of all", baseUrl);
                return []; // Empty = assume all need sync (safe when fetch fails)
            }
        }

        _logger.LogInformation("Fetched {Count} user IDs present on all reader(s)", intersection?.Count ?? 0);
        return intersection ?? [];
    }

    private async Task<HashSet<string>> FetchUserIdsFromReaderAsync(string baseUrl, CancellationToken cancellationToken)
    {
        var baseUrlNormalized = baseUrl.TrimEnd('/');
        var requestPath = "/ISAPI/AccessControl/UserInfo/Search?format=json";
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var position = 0;
        const int pageSize = 500;

        var clientOptions = new RestClientOptions(baseUrlNormalized)
        {
            Authenticator = new DigestAuthenticator(_options.Username, _options.Password),
            ThrowOnAnyError = false,
            Timeout = TimeSpan.FromSeconds(60)
        };

        using var client = new RestClient(clientOptions);

        while (true)
        {
            var searchRequest = new HikvisionUserInfoSearchRequest
            {
                SearchCond = new UserInfoSearchCond
                {
                    SearchId = Guid.NewGuid().ToString("N"),
                    SearchResultPosition = position,
                    MaxResults = pageSize
                }
            };
            var body = JsonSerializer.Serialize(searchRequest, JsonOptions);

            var request = new RestRequest(requestPath, Method.Post)
                .AddStringBody(body, DataFormat.Json)
                .AddHeader("Content-Type", "application/json");

            var response = await client.ExecuteAsync(request, cancellationToken);

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                _logger.LogWarning("UserInfo Search failed: {StatusCode} - {Content}", response.StatusCode, response.Content);
                throw new HttpRequestException($"UserInfo Search failed: {response.StatusCode}");
            }

            var searchResponse = JsonSerializer.Deserialize<HikvisionUserInfoSearchResponse>(response.Content!, JsonOptions);
            var userList = searchResponse?.SearchResult?.UserInfo;

            if (userList is null or { Count: 0 })
                break;

            foreach (var user in userList)
            {
                if (!string.IsNullOrWhiteSpace(user.EmployeeNo))
                    ids.Add(user.EmployeeNo);
            }

            var totalMatch = searchResponse?.SearchResult?.TotalMatchNum ?? 0;
            position += userList.Count;

            if (position >= totalMatch || userList.Count < pageSize)
                break;
        }

        return ids;
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
