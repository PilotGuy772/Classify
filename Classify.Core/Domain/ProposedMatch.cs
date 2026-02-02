namespace Classify.Core.Domain;

public class ProposedMatch
{
    public int Id { get; set; }
    public int AudioFileId { get; set; }

    // Optional FK references (if user selected existing entities)
    public int? ComposerId { get; set; }
    public int? WorkId { get; set; }
    public int? RecordingId { get; set; }
    public int? MovementId { get; set; }

    // Proposed work data (free-entry values when above IDs are not supplied)
    public string? ComposerName { get; set; }
    public string? WorkTitle { get; set; }
    public string? CatalogNumber { get; set; }

    // Proposed recording data / conductor
    public string? ConductorName { get; set; }

    // Proposed track data
    public int? MovementNumber { get; set; }
    public string? MovementTitle { get; set; }

    // Playback / performance metadata provided by user
    public int? PerformanceOrder { get; set; }

    // Match metadata
    public string Source { get; set; } = string.Empty;  // "MusicBrainz", "AI", "Manual", etc.
    public string? ExternalId { get; set; }              // MusicBrainz ID, etc.
    public float ConfidenceScore { get; set; }           // 0.0 - 1.0
    public string? MatchReasoning { get; set; }          // Why this match was proposed
    public bool Confirmed { get; set; }
}