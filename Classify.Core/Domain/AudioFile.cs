using Classify.Core.Enums;

namespace Classify.Core.Domain;

public class AudioFile
{
    public int Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public int RecordingId { get; set; }
    public int MovementId { get; set; }
    public IngestionStatus Status { get; set; }
}