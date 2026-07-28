namespace Classify.Core.Domain;

/// <summary>
/// Represents a playable unit linking a musical work and a recording for queues and playlists.
/// </summary>
public class Nibble
{
    /// <summary>
    /// Gets or sets the unique identifier for the nibble.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the associated work.
    /// </summary>
    public int WorkId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the associated recording.
    /// </summary>
    public int RecordingId { get; set; }

    /// <summary>
    /// Returns a string representation of the nibble.
    /// </summary>
    /// <returns>A string identifying the nibble.</returns>
    public override string ToString() => $"Nibble #{Id} (Work: {WorkId}, Recording: {RecordingId})";
}
