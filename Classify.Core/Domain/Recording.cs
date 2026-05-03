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
    /// Intended for use with works that primarily feature soloists, like concerti.
    /// For solo works such as solo sonatas, the soloist is the performer and should be set here.
    /// </summary>
    public string? Soloist { get; set; }

    /// <summary>
    /// Gets or sets the year this recording was made.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Returns a string representation of the recording.
    /// </summary>
    /// <returns>If set, in order:
    /// * The soloist
    /// * The conductor
    /// * The performing ensemble
    /// * The year of performance
    /// 
    /// Separated by Unicode "\u00B7" (middle dot) characters. (·)</returns>
    public override string ToString() =>
        string.Join(" \u00B7 ", ((List<object?>)[Soloist, Conductor, Ensemble, Year]).Where(x => x is not null));

    /// <summary>
    /// Gets the display name for the recording.
    /// </summary>
    public string Name => string.IsNullOrWhiteSpace(ToString()) ? $"Recording #{Id}" : ToString();

    /// <summary>
    /// Gets the photo key for the recording, if any.
    /// </summary>
    public string? PhotoKey => null;

    /// <summary>
    /// Gets the base object for IPlayable implementation.
    /// </summary>
    public object BaseObject => this;
}