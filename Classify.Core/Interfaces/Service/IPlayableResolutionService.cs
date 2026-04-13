using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Service;

public interface IPlayableResolutionService
{
    Task<PlayableSummary> ResolveSummaryAsync(IPlayable playable, CancellationToken ct = default);
    Task<IReadOnlyList<int>> ResolveAudioFileIdsAsync(IPlayable playable, CancellationToken ct = default);
    Task<PlayableResolution> ResolveAsync(IPlayable playable, CancellationToken ct = default);
}

