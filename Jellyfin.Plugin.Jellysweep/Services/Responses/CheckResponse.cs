using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Jellysweep.Services.Responses;

/// <summary>
/// Response model for media deletion check.
/// </summary>
public class CheckResponse
{
    /// <summary>
    /// Gets or sets a value indicating when the item is scheduled for deletion.
    /// </summary>
    [JsonPropertyName("deletion_date")]
    public DateTimeOffset? DeletionDate { get; set; }
}
