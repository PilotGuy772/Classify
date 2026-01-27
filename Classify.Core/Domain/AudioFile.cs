using Classify.Core.Enums;

namespace Classify.Core.Domain;

public class AudioFile
{
    public int Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public ulong Hash { get; set; }
    public IngestionStatus Status { get; set; }
}