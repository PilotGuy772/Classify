using Classify.Core.Domain;
using Classify.Core.Interfaces.Infrastructure;
using Classify.Core.Interfaces.Service;

namespace Classify.Data.Services;

public sealed class PlayablePlaylistService(IUnitOfWork uow, IPlayableResolutionService resolver) : IPlayablePlaylistService
{
    public async Task<IReadOnlyList<string>> BuildPlaylistPathsAsync(IPlayable playable, CancellationToken ct = default)
    {
        IReadOnlyList<int> ids = await resolver.ResolveAudioFileIdsAsync(playable, ct);
        if (ids.Count == 0) return Array.Empty<string>();

        IReadOnlyList<Classify.Core.Domain.AudioFile> files = await uow.AudioFiles.GetByIdsOrderedAsync(ids, ct);
        return files.Select(a => a.Path).ToList();
    }
}

