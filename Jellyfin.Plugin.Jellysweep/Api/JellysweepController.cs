using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
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
    /// Tests the connection to the Jellysweep API.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The connection test response.</returns>
    [HttpGet("testconnection")]
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
