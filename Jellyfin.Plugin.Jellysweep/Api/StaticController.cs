using System.Net.Mime;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Jellysweep.Api;

/// <summary>
/// API controller to serve static files for the Jellysweep plugin.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StaticController"/> class.
/// </remarks>
[ApiController]
[Route("Plugins/Jellysweep/Static")]
[Produces(MediaTypeNames.Application.Json)]
public class StaticController : ControllerBase
{
    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private readonly string _jellysweepLogoPath = $"{Assembly.GetExecutingAssembly().GetName().Name}.Web.jellysweep.png";

    /// <summary>
    /// Get the logo image for the Jellysweep plugin.
    /// </summary>
    /// <response code="200">Logo image successfully returned.</response>
    /// <response code="404">File not found.</response>
    /// <returns>The "jellysweep.png" embedded file.</returns>
    [HttpGet("Logo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("image/png")]
    public ActionResult GetLogo()
    {
        var logoStream = _assembly.GetManifestResourceStream(_jellysweepLogoPath);

        if (logoStream != null)
        {
            return File(logoStream, "image/png");
        }

        return NotFound();
    }
}
