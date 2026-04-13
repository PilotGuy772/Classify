using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Service;

public sealed class PlayableSummary
{
    public required IPlayable Playable { get; init; }
    public required string DisplayName { get; init; }
    public string? PhotoKey { get; init; }
    public object BaseObject => Playable.BaseObject;
}

