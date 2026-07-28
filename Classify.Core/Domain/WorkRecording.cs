namespace Classify.Core.Domain;

/// <summary>
/// Represents a join table record connecting a musical work and a recording.
/// </summary>
public class WorkRecording
{
    /// <summary>
    /// Gets or sets the unique identifier for the work-recording join.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the work.
    /// </summary>
    public int WorkId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the recording.
    /// </summary>
    public int RecordingId { get; set; }

    /// <summary>
    /// Gets the display name for the work recording join.
    /// </summary>
    public string Name => $"WorkRecording #{Id}";

    /// <summary>
    /// Gets the photo key for the work recording join, if any.
    /// </summary>
    public string? PhotoKey => null;
}
