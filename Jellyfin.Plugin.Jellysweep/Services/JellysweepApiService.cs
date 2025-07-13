using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellysweep.Services;

/// <summary>
/// Service for interacting with the Jellysweep API.
/// </summary>
public class JellysweepApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellysweepApiService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cache">The memory cache.</param>
    public JellysweepApiService(HttpClient httpClient, ILogger logger, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Tests the connection to the Jellysweep API.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with a boolean result indicating if the connection is successful.</returns>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var config = JellysweepPlugin.Instance?.Configuration;
        if (config == null || string.IsNullOrEmpty(config.JellysweepApiUrl) || string.IsNullOrEmpty(config.JellysweepApiKey))
        {
            return false;
        }

        try
        {
            var apiUrl = $"{config.JellysweepApiUrl.TrimEnd('/')}/plugin/health";
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Add("X-API-Key", config.JellysweepApiKey);
            request.Headers.Add("User-Agent", "Jellyfin-Plugin-Jellysweep/1.0.0");

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Jellysweep API connection");
            return false;
        }
    }
}
