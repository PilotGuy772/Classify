namespace Classify.Core.Domain.Infrastructure;

/// <summary>
/// Represents an authoritative data match inputted by a user.
/// </summary>
public class UserInputtedMatch
{
    // IDs referring to database objects if entities already exist in DB for this match
    // Priorities are in downward order: 
    // - A Recording's WorkId overrides this WorkId
    // - A Work's ComposerId overrides this ComposerId
    public int? ComposerId { get; set; }
    public int? WorkId { get; set; }
    public int? RecordingId { get; set; }
    public int? MovementId { get; set; }
    
    // This is free entry data which would be used to create NEW entities in DB if the above IDs are left blank.
    // These are ignored if above IDs are present.
    public string? ComposerName { get; set; } // Composer info
    public string? WorkName { get; set; } // Work info
    public string? CatalogNumber { get; set; } 
    public string? MovementName { get; set; } // Movement info
    public int? MovementNumber { get; set; }
    public string? ConductorName { get; set; } // Recording ingo
    public int? PerformanceOrder { get; set; }
    
}