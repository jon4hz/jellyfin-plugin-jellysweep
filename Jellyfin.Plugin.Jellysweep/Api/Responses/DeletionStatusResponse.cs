namespace Jellyfin.Plugin.Jellysweep.Api.Responses;

/// <summary>
/// Response model for item deletion status.
/// </summary>
public class DeletionStatusResponse
{
    /// <summary>
    /// Gets or sets the deletion date.
    /// </summary>
    public DateTimeOffset? DeletionDate { get; set; }

    /// <summary>
    /// Gets or sets the human-readable time until deletion.
    /// </summary>
    public string? HumanizedTimeUntilDeletion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is marked for deletion.
    /// </summary>
    public bool IsMarkedForDeletion { get; set; }
}
