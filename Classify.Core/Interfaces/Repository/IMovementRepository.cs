using Classify.Core.Domain;

namespace Classify.Core.Interfaces.Repository;

public interface IMovementRepository : IRepository<Movement>
{
    public Task<IEnumerable<Movement>> GetMovementsByWorkIdAsync(int id);
}