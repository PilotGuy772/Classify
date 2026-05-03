namespace Classify.Core.Domain;

/// <summary>
/// Represents a recording of a musical work.
/// </summary>
public class Recording : IPlayable
{
    /// <summary>
    /// Gets or sets the unique identifier for the recording.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the work being recorded.
    /// </summary>
    public int WorkId { get; set; }

    /// <summary>
    /// Gets or sets the conductor of the recording.
    /// </summary>
    public string Conductor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ensemble (e.g. orchestra, choir) for this recording.
    /// </summary>
    public string? Ensemble { get; set; }

    /// <summary>
    /// Gets or sets the soloist for this recording.
    /// </summary>
    public string? Soloist { get; set; }

    /// <summary>
    /// Gets or sets the year this recording was made.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Returns a string representation of the recording.
    /// </summary>
    /// <returns>The conductor's name.</returns>
    public override string ToString() => Conductor;

    /// <summary>
    /// Gets the display name for the recording.
    /// </summary>
    public string Name => string.IsNullOrWhiteSpace(Conductor) ? $"Recording #{Id}" : Conductor;

    /// <summary>
    /// Gets the photo key for the recording, if any.
    /// </summary>
    public string? PhotoKey => null;

    /// <summary>
    /// Gets the base object for IPlayable implementation.
    /// </summary>
    public object BaseObject => this;
}