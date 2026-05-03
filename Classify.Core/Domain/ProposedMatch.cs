namespace Classify.Core.Domain;

/// <summary>
/// Represents a proposed match for an audio file, containing metadata either from external sources or manual entry.
/// </summary>
public class ProposedMatch
{
    /// <summary>
    /// Gets or sets the unique identifier for the proposed match.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the audio file associated with this match.
    /// </summary>
    public int AudioFileId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the existing composer, if matched.
    /// </summary>
    public int? ComposerId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the existing work, if matched.
    /// </summary>
    public int? WorkId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the existing recording, if matched.
    /// </summary>
    public int? RecordingId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the existing movement, if matched.
    /// </summary>
    public int? MovementId { get; set; }

    /// <summary>
    /// Gets or sets the proposed name of the composer.
    /// </summary>
    public string? ComposerName { get; set; }

    /// <summary>
    /// Gets or sets the proposed title of the work.
    /// </summary>
    public string? WorkTitle { get; set; }

    /// <summary>
    /// Gets or sets the proposed catalog number for the work.
    /// </summary>
    public string? CatalogNumber { get; set; }

    /// <summary>
    /// Gets or sets the proposed conductor name.
    /// </summary>
    public string? ConductorName { get; set; }

    /// <summary>
    /// Gets or sets the proposed ensemble name.
    /// </summary>
    public string? EnsembleName { get; set; }

    /// <summary>
    /// Gets or sets the proposed soloist name.
    /// </summary>
    public string? SoloistName { get; set; }

    /// <summary>
    /// Gets or sets the proposed recording year.
    /// </summary>
    public int? RecordingYear { get; set; }

    /// <summary>
    /// Gets or sets the proposed movement number.
    /// </summary>
    public int? MovementNumber { get; set; }

    /// <summary>
    /// Gets or sets the proposed movement title.
    /// </summary>
    public string? MovementTitle { get; set; }

    /// <summary>
    /// Gets or sets the performance order for this track.
    /// </summary>
    public int? PerformanceOrder { get; set; }

    /// <summary>
    /// Gets or sets the source of the proposed match (e.g., "MusicBrainz", "Manual").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the external identifier for the match (e.g., MusicBrainz ID).
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Gets or sets the confidence score of the match proposal (0.0 to 1.0).
    /// </summary>
    public float ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets the reasoning behind why this match was proposed.
    /// </summary>
    public string? MatchReasoning { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the match has been confirmed by the user.
    /// </summary>
    public bool Confirmed { get; set; }
}