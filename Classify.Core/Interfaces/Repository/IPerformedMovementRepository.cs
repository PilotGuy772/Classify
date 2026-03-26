using Classify.Core.Domain;
using Classify.Core.Interfaces.Service;

namespace Classify.Core.Interfaces.Repository;

public interface IPerformedMovementRepository : IRepository<PerformedMovement>
{
    public Task<IEnumerable<PerformedMovement>> GetByMovementId(int id);
    public Task<IEnumerable<PerformedMovement>> GetByRecordingId(int id);

    Task<PlayableResolution> GetPlayableResolutionByIdAsync(int performedMovementId, CancellationToken ct = default);
}