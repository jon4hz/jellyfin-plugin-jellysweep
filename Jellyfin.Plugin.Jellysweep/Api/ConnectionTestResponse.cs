namespace Jellyfin.Plugin.Jellysweep.Api;

/// <summary>
/// Response model for connection test.
/// </summary>
public class ConnectionTestResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the connection is successful.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the test was performed.
    /// </summary>
    public DateTimeOffset TestedAt { get; set; }

    /// <summary>
    /// Gets or sets the test result message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
