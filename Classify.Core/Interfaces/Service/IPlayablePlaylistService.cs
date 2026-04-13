using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Service;

public interface IPlayablePlaylistService
{
    Task<IReadOnlyList<string>> BuildPlaylistPathsAsync(IPlayable playable, CancellationToken ct = default);
}

