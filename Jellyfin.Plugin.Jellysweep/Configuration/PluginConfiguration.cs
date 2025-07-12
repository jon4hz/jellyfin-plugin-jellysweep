using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Jellysweep.Configuration;

/// <summary>
/// Plugin configuration for Jellysweep.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the Jellysweep API URL.
    /// </summary>
    public string JellysweepApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Jellysweep API key.
    /// </summary>
    public string JellysweepApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the plugin is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache duration in minutes for Jellysweep API responses.
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;
}
