using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Repository;

public interface IPerformedMovementRepository : IRepository<PerformedMovement>
{
    public Task<IEnumerable<PerformedMovement>> GetByMovementId(int id);
    public Task<IEnumerable<PerformedMovement>> GetByRecordingId(int id);
}