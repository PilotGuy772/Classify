namespace Classify.Core.Domain;

/// <summary>
/// Represents a musical work (piece) in the library.
/// </summary>
public class Work
{
    /// <summary>
    /// Gets or sets the unique identifier for the work.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the composer who wrote the work.
    /// </summary>
    public int ComposerId { get; set; }

    /// <summary>
    /// Gets or sets the name/title of the work.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the catalog number (e.g. Op., BWV, K.) for the work.
    /// </summary>
    public string CatalogNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the favorite recording for this work.
    /// </summary>
    public int? FavoriteRecordingId { get; set; }

    /// <summary>
    /// Returns a string representation of the work.
    /// </summary>
    /// <returns>The name of the work.</returns>
    public override string ToString() => Name;
}