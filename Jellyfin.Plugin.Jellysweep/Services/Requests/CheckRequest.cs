using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Jellysweep.Services.Requests;

/// <summary>
/// Request model for media deletion check.
/// </summary>
public class CheckRequest
{
    /// <summary>
    /// Gets or sets the Name of the media item to check for deletion.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the Production Year of the media item to check for deletion.
    /// </summary>
    [JsonPropertyName("production_year")]
    public int ProductionYear { get; set; }

    /// <summary>
    /// Gets or sets the type of the media item to check for deletion.
    /// </summary>
    [JsonPropertyName("media_type")]
    public required string Type { get; set; }
}
