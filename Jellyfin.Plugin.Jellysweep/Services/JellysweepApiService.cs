using System.Text;
using System.Text.Json;
using Humanizer;
using Humanizer.Localisation;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Jellysweep.Services.Requests;
using Jellyfin.Plugin.Jellysweep.Services.Responses;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellysweep.Services;

/// <summary>
/// Service for interacting with the Jellysweep API.
/// </summary>
/// /// <param name="libraryManager">The library manager.</param>
/// <param name="logger">The logger.</param>
public class JellysweepApiService(
    ILibraryManager libraryManager,
    ILogger<JellysweepApiService> logger)
{
    private const string UserAgent = "Jellyfin-Plugin-Jellysweep/1.0.0";
    private const string ApiKeyHeader = "X-API-Key";
    private readonly ILibraryManager _libraryManager = libraryManager;
    private readonly ILogger<JellysweepApiService> _logger = logger;

    private static readonly HttpClient _httpClient = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JellysweepApiService"/> class.
    /// </summary>

    /// <summary>
    /// Generic method for making API calls to the Jellysweep API.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="endpoint">The API endpoint (relative to base URL).</param>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="requestBody">The request body (optional).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the response data or null if failed.</returns>
    private async Task<TResponse?> MakeApiCallAsync<TRequest, TResponse>(
        string endpoint,
        HttpMethod method,
        TRequest? requestBody = default,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class
    {
        var config = JellysweepPlugin.Instance?.Configuration;
        if (config == null || string.IsNullOrEmpty(config.JellysweepApiUrl) || string.IsNullOrEmpty(config.JellysweepApiKey))
        {
            _logger.LogWarning("Jellysweep API configuration is missing or incomplete");
            return null;
        }

        try
        {
            var apiUrl = $"{config.JellysweepApiUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            using var request = new HttpRequestMessage(method, apiUrl);

            // Set headers
            request.Headers.Add(ApiKeyHeader, config.JellysweepApiKey);
            request.Headers.Add("User-Agent", UserAgent);

            // Add request body if provided and method supports it
            if (requestBody != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
            {
                var json = JsonSerializer.Serialize(requestBody);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API call to {Endpoint} failed with status {StatusCode}", endpoint, response.StatusCode);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(responseContent))
            {
                return null;
            }

            return JsonSerializer.Deserialize<TResponse>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to make API call to {Endpoint}", endpoint);
            return null;
        }
    }

    /// <summary>
    /// Generic method for making API calls to the Jellysweep API without request body.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="endpoint">The API endpoint (relative to base URL).</param>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the response data or null if failed.</returns>
    private async Task<TResponse?> MakeApiCallAsync<TResponse>(
        string endpoint,
        HttpMethod method,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        return await MakeApiCallAsync<object, TResponse>(endpoint, method, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Generic method for making API calls to the Jellysweep API that don't return data.
    /// </summary>
    /// <param name="endpoint">The API endpoint (relative to base URL).</param>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with a boolean indicating success.</returns>
    private async Task<bool> MakeApiCallAsync(
        string endpoint,
        HttpMethod method,
        CancellationToken cancellationToken = default)
    {
        var config = JellysweepPlugin.Instance?.Configuration;
        if (config == null || string.IsNullOrEmpty(config.JellysweepApiUrl) || string.IsNullOrEmpty(config.JellysweepApiKey))
        {
            _logger.LogWarning("Jellysweep API configuration is missing or incomplete");
            return false;
        }

        try
        {
            var apiUrl = $"{config.JellysweepApiUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
            using var request = new HttpRequestMessage(method, apiUrl);

            // Set headers
            request.Headers.Add(ApiKeyHeader, config.JellysweepApiKey);
            request.Headers.Add("User-Agent", UserAgent);

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to make API call to {Endpoint}", endpoint);
            return false;
        }
    }

    /// <summary>
    /// Tests the connection to the Jellysweep API.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with a boolean result indicating if the connection is successful.</returns>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await MakeApiCallAsync("plugin/health", HttpMethod.Get, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if a media item is marked for deletion.
    /// </summary>
    /// <param name="itemId">The ID of the media item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation return the deletion date as string or null if not marked for deletion.</returns>
    public async Task<string?> IsItemMarkedForDeletionAsync(string itemId, CancellationToken cancellationToken = default)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item == null)
        {
            _logger.LogWarning("Item with ID {ItemId} not found", itemId);
            return null;
        }

        // Create a CheckRequest based on the item
        var checkRequest = new CheckRequest
        {
            Name = item.Name,
            Type = item.GetBaseItemKind() == BaseItemKind.Movie ? "movie" : "tv",
            ProductionYear = item.ProductionYear ?? 0
        };

        var response = await MakeApiCallAsync<CheckRequest, CheckResponse>("plugin/check", HttpMethod.Post, checkRequest, cancellationToken).ConfigureAwait(false);
        if (response?.DeletionDate == null)
        {
            return null;
        }

        var timeUntilDeletion = response.DeletionDate.Value.Subtract(DateTime.UtcNow);
        return timeUntilDeletion.Humanize(precision: 1, minUnit: TimeUnit.Day);
    }
}
