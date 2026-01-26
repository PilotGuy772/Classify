namespace Classify.Core.Domain;

public class ProposedMatch
{
    public int Id { get; set; }
    public int AudioFileId { get; set; }
    
    // Proposed work data
    public string? ComposerName { get; set; }
    public string? WorkTitle { get; set; }
    public string? CatalogNumber { get; set; }
    
    // Proposed recording data
    public string? ConductorName { get; set; }
    
    // Proposed track data
    public int? MovementNumber { get; set; }
    public string? MovementTitle { get; set; }
    
    // Match metadata
    public string Source { get; set; } = string.Empty;  // "MusicBrainz", "AI", "Manual", etc.
    public string? ExternalId { get; set; }              // MusicBrainz ID, etc.
    public float ConfidenceScore { get; set; }           // 0.0 - 1.0
    public string? MatchReasoning { get; set; }          // Why this match was proposed
    public bool Confirmed { get; set; } 
}