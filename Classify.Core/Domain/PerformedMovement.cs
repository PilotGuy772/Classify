namespace Classify.Core.Domain;

/// <summary>
/// Join type between Recording and Movement
/// </summary>
public class PerformedMovement : IPlayable
{
    public int Id { get; set; }
    public int RecordingId { get; set; }
    public int MovementId { get; set; }
    public int AudioFileId { get; set; }
    public int Order { get; set; }

    public string Name => $"Performed Movement #{Id}";

    public string? PhotoKey => null;

    public object BaseObject => this;
}