using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;

namespace Classify.Data.Services;

public sealed class PlayableResolutionService(IUnitOfWork uow) : IPlayableResolutionService
{
    public async Task<PlayableSummary> ResolveSummaryAsync(IPlayable playable, CancellationToken ct = default)
    {
        PlayableResolution resolution = await ResolveAsync(playable, ct);
        return new PlayableSummary
        {
            Playable = playable,
            DisplayName = resolution.DisplayName,
            PhotoKey = resolution.PhotoKey
        };
    }

    public async Task<IReadOnlyList<int>> ResolveAudioFileIdsAsync(IPlayable playable, CancellationToken ct = default)
    {
        PlayableResolution res = await ResolveAsync(playable, ct);
        return res.OrderedAudioFileIds;
    }

    public async Task<PlayableResolution> ResolveAsync(IPlayable playable, CancellationToken ct = default)
    {
        return playable switch
        {
            Recording r => await uow.Recordings.GetPlayableResolutionByIdAsync(r.Id, ct),
            PerformedMovement pm => await uow.PerformedMovements.GetPlayableResolutionByIdAsync(pm.Id, ct),
            _ => new PlayableResolution
            {
                DisplayName = playable.Name,
                PhotoKey = playable.PhotoKey,
                OrderedAudioFileIds = []
            }
        };
    }
}

