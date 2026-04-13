namespace Classify.Core.Interfaces.Service;

public sealed class PlayableResolution
{
    public required string DisplayName { get; init; }
    public string? PhotoKey { get; init; }
    public required IReadOnlyList<int> OrderedAudioFileIds { get; init; }
}

