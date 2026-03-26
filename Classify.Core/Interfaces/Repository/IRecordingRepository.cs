using Classify.Core.Domain;
using Classify.Core.Interfaces.Service;

namespace Classify.Core.Interfaces.Repository;

public interface IRecordingRepository : IRepository<Recording>
{
    public Task<IEnumerable<Recording>> GetRecordingsByWorkIdAsync(int id);

    Task<PlayableResolution> GetPlayableResolutionByIdAsync(int recordingId, CancellationToken ct = default);

    /// <summary>
    /// Find recordings by free-text query (conductor or other metadata).
    /// </summary>
    Task<IEnumerable<Recording>> FindByTextAsync(string query, int limit = 15, CancellationToken ct = default);
}