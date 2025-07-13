using System.Net.Mime;
using Humanizer;
using Humanizer.Localisation;
using Jellyfin.Plugin.Jellysweep.Api.Responses;
using Jellyfin.Plugin.Jellysweep.Services;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Jellysweep.Api;

/// <summary>
/// API controller for Jellysweep integration.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JellysweepController"/> class.
/// </remarks>
/// <param name="libraryManager">The library manager.</param>
/// <param name="logger">The logger.</param>
/// <param name="loggerFactory">The logger factory.</param>
[ApiController]
[Route("Plugins/Jellysweep")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class JellysweepController(
    ILibraryManager libraryManager,
    ILogger<JellysweepController> logger,
    ILoggerFactory loggerFactory) : ControllerBase
{
    private readonly ILibraryManager _libraryManager = libraryManager;
    private readonly ILogger<JellysweepController> _logger = logger;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private static readonly HttpClient _httpClient = new();
    private static readonly MemoryCache _cache = new(new MemoryCacheOptions());

    /// <summary>
    /// Check if a media item is marked for deletion by Jellysweep.
    /// </summary>
    /// <param name="itemId">The ID of the media item.</param>
    /// <returns>The deletion status information including human-readable time format.</returns>
    /// <response code="200">Returns the deletion status information.</response>
    /// <response code="404">If the item is not found.</response>
    /// <response code="500">If an error occurs while checking the item.</response>
    [HttpGet("IsItemMarkedForDeletion/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<DeletionStatusResponse> IsItemMarkedForDeletion(string itemId)
    {
        try
        {
            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            // Dummy data: return in 30 days for now
            var deletionDate = DateTimeOffset.UtcNow.AddDays(30);
            var timeUntilDeletion = deletionDate - DateTimeOffset.UtcNow;

            return Ok(new DeletionStatusResponse
            {
                IsMarkedForDeletion = true,
                DeletionDate = deletionDate,
                HumanizedTimeUntilDeletion = timeUntilDeletion.Humanize(precision: 1, minUnit: TimeUnit.Day) // Show up to 2 time units
            });

            // Check if the item is marked for deletion by Jellysweep
            /* var isMarkedForDeletion = item.GetUserData()?.GetValue("JellysweepMarkedForDeletion") as bool? ?? false;
            return Ok(isMarkedForDeletion); */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if item {ItemId} is marked for deletion", itemId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while checking the item.");
        }
    }

    /// <summary>
    /// Tests the connection to the Jellysweep API.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The connection test response.</returns>
    [HttpGet("TestConnection")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ConnectionTestResponse>> TestConnection(CancellationToken cancellationToken)
    {
        _ = _libraryManager; // to avoid unused variable warning for now
        try
        {
            var serviceLogger = _loggerFactory.CreateLogger<JellysweepApiService>();
            var jellysweepService = new JellysweepApiService(_httpClient, serviceLogger, _cache);
            var isConnected = await jellysweepService.TestConnectionAsync(cancellationToken).ConfigureAwait(false);

            return Ok(new ConnectionTestResponse
            {
                IsConnected = isConnected,
                TestedAt = DateTimeOffset.UtcNow,
                Message = isConnected ? "Connection successful" : "Connection failed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Jellysweep API connection");
            return Ok(new ConnectionTestResponse
            {
                IsConnected = false,
                TestedAt = DateTimeOffset.UtcNow,
                Message = $"Connection test failed: {ex.Message}"
            });
        }
    }
}
