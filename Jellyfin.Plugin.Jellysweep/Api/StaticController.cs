using System.Net.Mime;
using System.Reflection;
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
    private readonly string _jellysweepScriptPath = $"{Assembly.GetExecutingAssembly().GetName().Name}.Web.jellysweep.js";
    private readonly string _jellysweepLogoPath = $"{Assembly.GetExecutingAssembly().GetName().Name}.Web.jellysweep.png";

    /// <summary>
    /// Get the javascript file for the Jellysweep plugin.
    /// </summary>
    /// <response code="200">Javascript file successfully returned.</response>
    /// <response code="404">File not found.</response>
    /// <returns>The "jellysweep.js" embedded file.</returns>
    [HttpGet("ClientScript")]
    public ActionResult GetClientScript()
    {
        var scriptStream = _assembly.GetManifestResourceStream(_jellysweepScriptPath);

        if (scriptStream != null)
        {
            return File(scriptStream, "application/javascript");
        }

        return NotFound();
    }

    /// <summary>
    /// Get the logo image for the Jellysweep plugin.
    /// </summary>
    /// <response code="200">Logo image successfully returned.</response>
    /// <response code="404">File not found.</response>
    /// <returns>The "jellysweep.png" embedded file.</returns>
    [HttpGet("Logo")]
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
