namespace Classify.Core.Domain;

/// <summary>
/// Join type between Recording and Movement
/// </summary>
public class PerformedMovement
{
    public int Id { get; set; }
    public int RecordingId { get; set; }
    public int MovementId { get; set; }
    public int Order { get; set; }
}